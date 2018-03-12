using System;
using System.Collections.Generic;
using System.Linq;
using ElasticEpiserver.Module.Business.Data;
using ElasticEpiserver.Module.Business.Data.Entities;
using ElasticEpiserver.Module.Business.Extensions;
using ElasticEpiserver.Module.Business.Helpers;
using ElasticEpiserver.Module.Engine.Client;
using ElasticEpiserver.Module.Engine.Indexing;
using ElasticEpiserver.Module.Engine.Models;
using ElasticEpiserver.Module.Engine.Parsing.Pages;
using EPiServer.ServiceLocation;
using Nest;

namespace ElasticEpiserver.Module.Engine.Search
{
    public class ElasticEpiSearch<T> where T : ElasticEpiDocument, new()
    {
        private ElasticEpiSearch()
        {
        }

        private static ElasticEpiSearch<T> _instance;

        public static ElasticEpiSearch<T> Current => _instance ?? (_instance = new ElasticEpiSearch<T>());

        public ElasticEpiSearchResults Search(ElasticEpiSearchParams searchParams)
        {
            var results = new ElasticEpiSearchResults();
            var excludedContentGuids = new List<string>();

            InsertBestBets(searchParams, results, excludedContentGuids);

            var response = PerformSearch(searchParams, excludedContentGuids);

            foreach (var hit in response.Hits)
            {
                var item = new ElasticEpiSearchResultItem { Document = hit.Source };

                if (hit.Score.HasValue)
                {
                    item.Score = hit.Score.Value;

                    results.Items.Add(item);
                }
            }

            results.Count += (int)response.Total;

            return results;
        }

        private static void InsertBestBets(ElasticEpiSearchParams searchParams, ElasticEpiSearchResults results, ICollection<string> contentGuids)
        {
            var bestBets = ServiceLocator.Current.GetInstance<IDynamicDataRepository<BestBet>>().ReadAll()
                .Where(bb => string.Equals(bb.Keyword, searchParams.Query, StringComparison.CurrentCultureIgnoreCase) &&
                             string.Equals(bb.Language, searchParams.LanguageName,
                                 StringComparison.CurrentCultureIgnoreCase));

            var bestBetValidator = searchParams.BestBetOptions.Validator;

            foreach (var bestBet in bestBets)
            {
                var contents = bestBet.Contents.OrderByDescending(c => c.Order).Skip(searchParams.Skip)
                    .Take(searchParams.Take).ToList();

                foreach (var content in contents)
                {
                    var page = PageResolver.ResolvePage(content.ContentGuid, searchParams.LanguageName);

                    if (bestBetValidator != null && !bestBetValidator.ShouldIncludeAsBestBet(page, searchParams.BestBetOptions))
                        continue;

                    var document = ServiceLocator.Current.GetInstance<IEpiPageParser>().Parse(page);

                    if (document != null)
                    {
                        contentGuids.Add(document.ContentGuid);

                        results.Items.Add(new ElasticEpiSearchResultItem
                        {
                            Document = document,
                            Score = double.MaxValue
                        });

                        results.Count++;

                        if (searchParams.Take > 0)
                            searchParams.Take--;
                    }
                }

                if (searchParams.Skip > 0)
                    searchParams.Skip -= contents.Count;

                if (searchParams.Skip < 0)
                    searchParams.Skip = 0;
            }
        }

        private ISearchResponse<T> PerformSearch(ElasticEpiSearchParams searchParams, ICollection<string> excludedContentGuids)
        {
            var indexName = ElasticEpiClient.Current.ContentIndexName(searchParams.LanguageName);

            var templateParameters = new Dictionary<string, object>
            {
                {ElasticEpiSearchTemplate<T>.ParamQuery, searchParams.Query},
                {ElasticEpiSearchTemplate<T>.ParamSkip, $"{searchParams.Skip}"},
                {ElasticEpiSearchTemplate<T>.ParamTake, $"{searchParams.Take}"}
            };

            foreach (var filter in searchParams.MatchFilters)
            {
                templateParameters.Add(filter.Key.ToElasticPropertyName(), filter.Value);
            }

            if (searchParams.IncludedTypes.Any())
            {
                templateParameters.Add(ElasticEpiSearchTemplate<T>.ParamIncludedTypeNames,
                    string.Join(",", searchParams.IncludedTypes));
            }

            if (searchParams.ExcludedTypes.Any())
            {
                templateParameters.Add(ElasticEpiSearchTemplate<T>.ParamExcludedTypeNames,
                    string.Join(",", searchParams.ExcludedTypes));
            }

            if (excludedContentGuids.Any())
            {
                templateParameters.Add(ElasticEpiSearchTemplate<T>.ParamExcludedIds,
                    string.Join(",", excludedContentGuids));
            }

            var response = ElasticEpiClient.Current.Get()
                .SearchTemplate<T>(e => e.Index(indexName)
                .AllTypes()
                .Params(templateParameters)
                .Id(ElasticEpiSearchTemplate<T>.Name));

            if (!searchParams.IsExcludedFromStatistics)
            {
                ServiceLocator.Current.GetInstance<IElasticIndexer<T>>()
                    .IndexSearchRequestForStatistics(searchParams.Query, searchParams.LanguageName, response.Total,
                        response.Took);
            }

            return response;
        }

        public IList<DocumentClickCount> SearchForDocumentClickCounts()
        {
            var indexName = ElasticEpiClient.Current.HitsLogIndexName;

            if (!ElasticEpiClient.Current.Get().IndexExists(indexName).Exists)
                return null;

            var result = new List<DocumentClickCount>();

            var response = ElasticEpiClient.Current.Get()
                .Search<ElasticHitLogDocument>(e => e
                    .Index(indexName)
                    .AllTypes()
                    .Aggregations(a => a.Terms("by_language", x => x.Field("language.keyword")
                        .Aggregations(b => b.Terms("documents",
                            y => y.Field("contentGuid.keyword").Size(int.MaxValue))))));

            if (response.IsValid && response.Aggregations.Any())
            {
                var languageGroup = response.Aggs.Terms("by_language");

                foreach (var languageBucket in languageGroup.Buckets)
                {
                    foreach (var languageAggregate in languageBucket.Aggregations)
                    {
                        var documentBucket = languageAggregate.Value as BucketAggregate;

                        if (documentBucket == null)
                            continue;

                        foreach (var documentAggregate in documentBucket.Items)
                        {
                            var bucket = documentAggregate as KeyedBucket<object>;

                            if (bucket == null)
                                continue;

                            var lang = languageBucket.Key;
                            var guid = bucket.Key.ToString();
                            var count = bucket.DocCount;

                            if (count.HasValue)
                            {
                                result.Add(new DocumentClickCount
                                {
                                    ContentGuid = new Guid(guid),
                                    Language = lang,
                                    Count = (int)count.Value
                                });
                            }
                        }
                    }
                }
            }

            return result;
        }
    }
}