using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using ElasticEpiserver.Module.Business.Data;
using ElasticEpiserver.Module.Business.Data.Entities;
using ElasticEpiserver.Module.Business.Extensions;
using ElasticEpiserver.Module.Engine.Client;
using ElasticEpiserver.Module.Engine.Parsing.Pages;
using ElasticEpiserver.Module.Engine.Search;
using EPiServer;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.ServiceLocation;
using Nest;
using static ElasticEpiserver.Module.Engine.Languages.ElasticEpiLanguageHelper;

namespace ElasticEpiserver.Module.Engine.Indexing
{
    public class ElasticIndexer<T> : IElasticIndexer<T> where T : ElasticEpiDocument, new()
    {
        private readonly IEpiPageParser _pageParser;
        private readonly IEpiContentValidator _contentValidator;

        public ElasticIndexer(IEpiPageParser pageParser, IEpiContentValidator contentValidator)
        {
            _pageParser = pageParser;
            _contentValidator = contentValidator;
        }

        public void RebuildIndex(Action<string> statusMessageCallback)
        {
            var pageTree = ContentLoader().GetDescendents(ContentReference.StartPage).ToList();
            var documents = new List<ElasticEpiDocument>();
            var tasks = new List<Task>();
            var indexedCounter = 0;
            var skippedCounter = 0;

            foreach (var contentReference in pageTree)
            {
                var task = Task.Run(() =>
                {
                    var content = ContentLoader().Get<IContent>(contentReference);

                    if (!_contentValidator.ShouldBeIndexed(content) || !(content is ISearchablePage))
                    {
                        skippedCounter++;
                        //continue;
                        return;
                    }

                    var inflated = InflateIndexableDocumentsForContent(content);

                    if (inflated.Any())
                    {
                        lock (documents)
                        {
                            documents.AddRange(inflated);
                        }
                    }

                    indexedCounter++;

                    statusMessageCallback(
                        $"Parsing content... {GetProgress(indexedCounter + skippedCounter, pageTree.Count)} (Skipped: {skippedCounter})");
                });

                tasks.Add(task);
            }

            Task.WaitAll(tasks.ToArray());

            IndexDocuments(documents, true, statusMessageCallback);
            UpdateClickCountOnDocuments();

            statusMessageCallback("Finished rebuilding indexes.");

            ElasticEpiSearchTemplate<T>.Rebuild();

            statusMessageCallback("Updated search template.");
        }

        public void UpdateEditorSynonyms()
        {
            foreach (var languageBranch in ServiceLocator.Current.GetInstance<ILanguageBranchRepository>().ListEnabled())
            {
                var indexName = ElasticEpiClient.Current.ContentIndexName(languageBranch.Culture.Name);

                if (!ElasticEpiClient.Current.Get().IndexExists(indexName).Exists)
                {
                    // Skip if we don't have any indexes with this name
                    continue;
                }

                ElasticEpiClient.Current.Get().CloseIndex(indexName);

                ElasticEpiClient.Current.Get().UpdateIndexSettings(null, i => i.Index(indexName)
                    .IndexSettings(idx => idx
                        .Analysis(a => a
                            .TokenFilters(tf => tf.Synonym(IndexSettingsFactory.Names.TokenFilters.EditorSynonyms, s => s
                                .Synonyms(SynonymHelper.ResolveSynonymsForLanguage(languageBranch.Culture.Name)))))));

                ElasticEpiClient.Current.Get().OpenIndex(indexName);
            }
        }

        public void IndexSearchRequestForStatistics(string query, string language, long totalHits, long time)
        {
            var document = new ElasticStatisticsDocument
            {
                Query = query,
                Language = language,
                ResponseTime = time,
                NumberOfResults = totalHits,
                SearchDate = DateTime.Now
            };

            ElasticEpiClient.Current.Get().Index(document,
                d => d.Index(ElasticEpiClient.Current.StatisticsIndexName));
        }

        public void IndexHitLogDocument(string query, string language, Guid contentGuid)
        {
            var document = new ElasticHitLogDocument
            {
                Query = query,
                Language = language,
                ContentGuid = contentGuid
            };

            ElasticEpiClient.Current.Get().Index(document, d => d.Index(ElasticEpiClient.Current.HitsLogIndexName));
        }

        public bool UpdateClickCountOnDocuments()
        {
            var documents = ElasticEpiSearch<T>.Current.SearchForDocumentClickCounts();

            if (documents == null || !documents.Any())
                return false;

            foreach (var document in documents)
            {
                var indexName = ElasticEpiClient.Current.ContentIndexName(document.Language);

                dynamic update = new ExpandoObject();

                // numClicks != NumClicks because dynamic object
                update.numClicks = document.Count;

                var response = ElasticEpiClient.Current.Get()
                    .Update<ElasticEpiDocument, dynamic>(new DocumentPath<ElasticEpiDocument>(document.ContentGuid),
                        e => e.Index(indexName).Doc(update));

                if (!response.IsValid)
                {
                    return false;
                }
            }

            return true;
        }

        public void OnContentPublished(IContent content)
        {
            var documents = new List<ElasticEpiDocument>();

            var localizable = content as ILocalizable;

            if (localizable == null)
                return;

            if (IsFallbackLanguageCandidate(localizable))
            {
                NorwegianCulture norwegianCulture = GetNorwegianCulture();

                // This content is in bokmål master language while it does not exist in nynorsk
                var document = ParsePage(ContentLoader().Get<IContent>(content.ContentLink, norwegianCulture.Bokmal));

                if (document != null && document.IsValid())
                {
                    document.LanguageName = norwegianCulture.Nynorsk.Name;
                    document.IsFallbackLanguage = bool.TrueString;

                    documents.Add(document);
                }
            }

            var page = ParsePage(content);

            if (page != null && page.IsValid())
            {
                documents.Add(page);
            }

            IndexDocuments(documents);
        }

        public void OnContentLanguageDeleted(IContent content)
        {
            var localizable = content as ILocalizable;

            if (localizable == null)
                return;

            var indexName = ElasticEpiClient.Current.ContentIndexName(localizable.Language.Name);

            if (ElasticEpiClient.Current.Get().IndexExists(indexName).Exists)
            {
                ElasticEpiClient.Current.Get()
                    .Delete<ElasticEpiDocument>(content.ContentGuid, i => i.Index(indexName));
            }

            DeleteContentFromBestBets(content, localizable.Language.Name);
        }

        public void OnContentDeleted(IContent content)
        {
            var localizable = content as ILocalizable;

            if (localizable == null)
                return;

            foreach (var culture in localizable.ExistingLanguages)
            {
                var indexName = ElasticEpiClient.Current.ContentIndexName(culture.Name);

                if (ElasticEpiClient.Current.Get().IndexExists(indexName).Exists)
                {
                    ElasticEpiClient.Current.Get()
                        .Delete<ElasticEpiDocument>(content.ContentGuid, i => i.Index(indexName));
                }
            }

            DeleteContentFromBestBets(content);
            DeleteContentFromHitsLogIndex(content);
        }

        private void DeleteContentFromBestBets(IContent content, string language = null)
        {
            // If content is deleted from the index, we want to remove it from any existing best bets
            var repository = ServiceLocator.Current.GetInstance<IDynamicDataRepository<BestBet>>();

            try
            {
                var bestBets = language != null
                    ? repository.ReadAll().Where(c => c.Language == language)
                    : repository.ReadAll();

                foreach (var bb in bestBets)
                {
                    var isContentCollectionModified = false;

                    // Reverse iterate to allow for deletion of expired content
                    for (var i = bb.Contents.Count - 1; i >= 0; i--)
                    {
                        var c = bb.Contents[i];

                        if (ContentLoader().Get<IContent>(c.ContentGuid) == content)
                        {
                            bb.Contents.RemoveAt(i);
                            isContentCollectionModified = true;
                        }
                    }

                    if (isContentCollectionModified)
                    {
                        repository.Update(bb);
                    }
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private void DeleteContentFromHitsLogIndex(IContent content)
        {
            var localizable = content as ILocalizable;

            if (localizable == null)
                return;

            try
            {
                var response = ElasticEpiClient.Current.Get().DeleteByQuery<ElasticHitLogDocument>(e => e
                    .Index(ElasticEpiClient.Current.HitsLogIndexName)
                    .Query(q => q
                        .Bool(b => b
                            .Must(p => p
                                .Match(m => m.Query(content.ContentGuid.ToString()).Field(f => f.ContentGuid))))));

                if (!response.IsValid)
                {

                }
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private void IndexDocuments(List<ElasticEpiDocument> documents, bool shouldReplaceIndexSettings = false, Action<string> statusMessageCallback = null)
        {
            var languageNames = documents.Select(d => d.LanguageName).Distinct();
            var tasks = new List<Task<IBulkResponse>>();

            var counter = 0;

            foreach (var languageName in languageNames)
            {
                var indexName = $"{ElasticEpiClient.Current.ContentIndexName(languageName)}";

                if (shouldReplaceIndexSettings)
                {
                    CreateOrReplaceIndexSettings(indexName, languageName);
                }

                var docs = documents.Where(d => d.LanguageName != null && d.LanguageName == languageName).ToList();

                // split documents by type and index them asynchronously to improve performance
                var typeNames = docs.Select(d => d.TypeName).Distinct();

                foreach (var typeName in typeNames)
                {
                    var typedDocuments = docs.Where(d => d.TypeName == typeName).ToList();

                    counter += typedDocuments.Count;

                    statusMessageCallback?.Invoke($"Indexing content... {GetProgress(counter, documents.Count)}");

                    tasks.Add(ElasticEpiClient.Current.Get()
                        .IndexManyAsync(typedDocuments, indexName, typeof(ElasticEpiDocument)));
                }

            }

            Task.WaitAll(tasks.Cast<Task>().ToArray());
        }

        private static string GetProgress(int current, int total)
        {
            return ((double)current / total).ToString("P");
        }

        private static void CreateOrReplaceIndexSettings(string indexName, string languageName)
        {
            if (ElasticEpiClient.Current.Get().IndexExists(indexName).Exists)
            {
                ElasticEpiClient.Current.Get().DeleteIndex(indexName);
            }

            ElasticEpiClient.Current.Get()
                .CreateIndex(indexName, index => index
                    .InitializeUsing(IndexSettingsFactory.GetByLanguageName(languageName))
                    .Mappings(m => m.Map<ElasticEpiDocument>(map => map
                        .AutoMap(0)
                        .Properties(prop =>
                        {
                            foreach (var propertyName in ServiceLocator.Current.GetInstance<ElasticEpiDocument>().GetLanguageAnalyzableProperties())
                            {
                                prop.Text(x => x.Name(propertyName.ToElasticPropertyName())
                                    .Fields(o => o
                                        .Text(oo => oo.Name(IndexSettingsFactory.Names.Analyzers.Shorthand.Language)
                                            .Analyzer(IndexSettingsFactory.Names.Analyzers.Language))
                                        .Text(oo => oo.Name(IndexSettingsFactory.Names.Analyzers.Shorthand.Ngram)
                                            .Analyzer(IndexSettingsFactory.Names.Analyzers.Ngram))));
                            }

                            return prop;
                        })
                    )));
        }

        private ElasticEpiDocument ParsePage(IContent content)
        {
            var page = content as PageData;
            var indexable = page as ISearchablePage;

            if (page != null && indexable != null)
            {
                return _pageParser.Parse(page);
            }

            return null;
        }

        private IList<ElasticEpiDocument> InflateIndexableDocumentsForContent(IContent content)
        {
            var documents = new List<ElasticEpiDocument>();

            var localizable = content as ILocalizable;

            if (localizable == null)
                return documents;

            if (IsFallbackLanguageCandidate(localizable))
            {
                NorwegianCulture norwegianCulture = GetNorwegianCulture();

                // This content is in bokmål master language while it does not exist in nynorsk
                var document = ParsePage(ContentLoader().Get<IContent>(content.ContentLink, norwegianCulture.Bokmal));

                if (document != null && document.IsValid())
                {
                    document.LanguageName = norwegianCulture.Nynorsk.Name;
                    document.IsFallbackLanguage = bool.TrueString;

                    documents.Add(document);
                }
            }

            foreach (var culture in localizable.ExistingLanguages)
            {
                var document = ParsePage(ContentLoader().Get<IContent>(content.ContentLink, culture));

                if (document == null)
                {
                    continue;
                }

                if (document.IsValid())
                {
                    documents.Add(document);
                }
            }


            return documents;
        }

        private IContentLoader ContentLoader()
        {
            return ServiceLocator.Current.GetInstance<IContentLoader>();
        }
    }
}