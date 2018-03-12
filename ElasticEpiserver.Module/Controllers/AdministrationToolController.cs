using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using ElasticEpiserver.Module.Business.Data;
using ElasticEpiserver.Module.Business.Data.Entities;
using ElasticEpiserver.Module.Business.Extensions;
using ElasticEpiserver.Module.Business.Initialization;
using ElasticEpiserver.Module.Engine.Indexing;
using ElasticEpiserver.Module.Engine.Languages;
using ElasticEpiserver.Module.Engine.Search;
using ElasticEpiserver.Module.Models;
using EPiServer;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.PlugIn;
using EPiServer.ServiceLocation;
using EPiServer.Shell;
using EPiServer.Web.Routing;
using Newtonsoft.Json;

namespace ElasticEpiserver.Module.Controllers
{
    [Authorize]
    [GuiPlugIn(Area = EPiServer.PlugIn.PlugInArea.AdminMenu,
        Url = "/elasticepi-admin/search-admin",
        DisplayName = "ElasticEpiserver - Administration Tool")]
    public class AdministrationToolController : Controller
    {
        private readonly IContentRepository _contentRepository;
        private readonly IContentLoader _contentLoader;
        private readonly IDynamicDataRepository<Decay> _decayRepository;
        private readonly IDynamicDataRepository<BestBet> _bestBetRepository;
        private readonly IDynamicDataRepository<SynonymContainer> _synonymContainerRepository;
        private readonly IDynamicDataRepository<WeightSetting> _weightSettingRepository;
        private readonly IDynamicDataRepository<Boost> _boostRepository;
        private readonly UrlResolver _urlResolver;

        public AdministrationToolController(IContentRepository contentRepository, IContentLoader contentLoader, IDynamicDataRepository<Decay> decayRepository,
            IDynamicDataRepository<BestBet> bestBetRepository, IDynamicDataRepository<SynonymContainer> synonymContainerRepository,
            IDynamicDataRepository<WeightSetting> weightSettingRepository, IDynamicDataRepository<Boost> boostRepository, UrlResolver urlResolver)
        {
            _contentRepository = contentRepository;
            _contentLoader = contentLoader;
            _decayRepository = decayRepository;
            _bestBetRepository = bestBetRepository;
            _synonymContainerRepository = synonymContainerRepository;
            _weightSettingRepository = weightSettingRepository;
            _boostRepository = boostRepository;
            _urlResolver = urlResolver;
        }

        public ActionResult Index()
        {
            return View(ResolveViewPath("Index"), ResolveViewModel());
        }

        #region Synonyms API
        [HttpPost]
        public ActionResult CreateSynonymContainer(string word, string synonyms, string language)
        {
            var synonymContainer = new SynonymContainer(word, language);

            if (synonyms != null)
            {
                var array = JsonConvert.DeserializeObject<string[]>(synonyms);

                if (array.Any())
                {
                    foreach (var synonym in array)
                    {
                        synonymContainer.Synonyms.Add(synonym);
                    }
                }
            }

            var isCreated = _synonymContainerRepository
                .CreateIfNotExisting(synonymContainer, x => x.Word == word && x.Language == language);

            return isCreated ? new HttpStatusCodeResult(HttpStatusCode.OK) : new HttpStatusCodeResult(HttpStatusCode.Forbidden);
        }

        [HttpPost]
        public ActionResult DeleteSynonymContainer(string word, string language)
        {
            var synonymContainer = _synonymContainerRepository.SingleOrDefault(x => x.Word == word && x.Language == language);

            if (synonymContainer != null)
            {
                _synonymContainerRepository.Delete(synonymContainer);

                return new HttpStatusCodeResult(HttpStatusCode.OK);
            }

            return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        }

        [HttpPost]
        public ActionResult AddSynonym(string word, string language, string synonym)
        {
            var synonymContainer = _synonymContainerRepository.SingleOrDefault(x => x.Word == word && x.Language == language);

            if (synonymContainer != null)
            {
                synonym = synonym.Trim();

                if (!synonymContainer.Synonyms.Contains(synonym))
                {
                    synonymContainer.Synonyms.Add(synonym);

                    _synonymContainerRepository.Update(synonymContainer);

                    return new HttpStatusCodeResult(HttpStatusCode.OK);
                }

                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            }

            return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        }

        [HttpPost]
        public ActionResult RemoveSynonym(string word, string language, string synonym)
        {
            var synonymContainer = _synonymContainerRepository.SingleOrDefault(x => x.Word == word && x.Language == language);

            if (synonymContainer != null)
            {
                synonymContainer.Synonyms.Remove(synonym);

                _synonymContainerRepository.Update(synonymContainer);

                return new HttpStatusCodeResult(HttpStatusCode.OK);
            }

            return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        }
        #endregion

        #region Boost API 
        [HttpPost]
        public ActionResult SaveBoost(Guid contentGuid, double rate)
        {
            var boost = _boostRepository.SingleOrDefault(i => i.ContentGuid == contentGuid);

            if (boost != null)
            {
                boost.Value = rate;

                _boostRepository.Update(boost);

                return new HttpStatusCodeResult(HttpStatusCode.OK);
            }

            return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        }
        #endregion

        #region Weight settings API
        [HttpPost]
        public ActionResult SavePropertyWeightItem(Guid? id, string property, double weight)
        {
            if (string.IsNullOrEmpty(property)) { throw new ArgumentNullException(nameof(property)); }

            WeightSetting entry;

            if (id == null)
            {
                entry = new WeightSetting(property, weight);
            }
            else
            {
                entry = _weightSettingRepository.ReadById(id.Value);
                entry.Property = property;
                entry.Weight = weight;
            }

            _weightSettingRepository.Update(entry);

            if (entry.Id != null)
            {
                return Json(entry);
            }

            return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        }

        [HttpPost]
        public ActionResult DeletePropertyWeightItem(Guid id)
        {
            try
            {
                var weightSetting = _weightSettingRepository.ReadById(id);

                if (weightSetting != null)
                {
                    _weightSettingRepository.Delete(weightSetting);
                }
            }
            catch (Exception)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            return new HttpStatusCodeResult(HttpStatusCode.OK);
        }
        #endregion

        #region Decay API
        [HttpPost]
        public ActionResult SaveDecay(Guid contentGuid, bool isActive, int days, double rate, int offset, double boost)
        {
            var decay = _decayRepository.SingleOrDefault(i => i.ContentGuid == contentGuid);

            if (decay != null)
            {
                decay.IsActive = isActive;
                decay.Scale = days;
                decay.Rate = rate;
                decay.Offset = offset;
                decay.Weight = boost;

                _decayRepository.Update(decay);

                return new HttpStatusCodeResult(HttpStatusCode.OK);
            }

            return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        }
        #endregion

        #region Best bets API
        [HttpPost]
        public ActionResult CreateBestBet(string keyword, string language)
        {
            var bestBet = new BestBet(keyword, language);

            var isCreated = _bestBetRepository.CreateIfNotExisting(bestBet, x => x.Keyword == keyword && x.Language == language);

            if (isCreated)
            {
                return Json(bestBet.Id.ExternalId);
            }

            return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        }

        [HttpPost]
        public ActionResult DeleteBestBet(string id)
        {
            var bestBet = _bestBetRepository.ReadById(new Guid(id));

            if (bestBet != null)
            {
                _bestBetRepository.Delete(bestBet);

                return new HttpStatusCodeResult(HttpStatusCode.OK);
            }

            return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        }

        [HttpPost]
        public ActionResult AddBestBetContent(string id, Guid contentGuid)
        {
            var bestBet = _bestBetRepository.ReadById(new Guid(id));

            if (bestBet != null)
            {
                if (bestBet.Contents.All(i => i.ContentGuid != contentGuid))
                {
                    bestBet.Contents.Add(new BestBetContent { ContentGuid = contentGuid, Order = 0 });

                    _bestBetRepository.Update(bestBet);

                    return new HttpStatusCodeResult(HttpStatusCode.OK);
                }

                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            }

            return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        }

        [HttpPost]
        public ActionResult RemoveBestBetContent(string id, Guid contentGuid)
        {
            var bestBet = _bestBetRepository.ReadById(new Guid(id));

            var content = bestBet?.Contents.SingleOrDefault(i => i.ContentGuid == contentGuid);

            if (bestBet != null)
            {
                bestBet.Contents.Remove(content);

                _bestBetRepository.Update(bestBet);

                return new HttpStatusCodeResult(HttpStatusCode.OK);
            }

            return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        }

        [HttpPost]
        public ActionResult OrderBestBetContents(string id, IDictionary<string, int> order)
        {
            var bestBet = _bestBetRepository.ReadById(new Guid(id));

            if (bestBet != null)
            {
                foreach (var content in bestBet.Contents)
                {
                    content.Order = order[content.ContentGuid.ToString()];
                }

                _bestBetRepository.Update(bestBet);

                return new HttpStatusCodeResult(HttpStatusCode.OK);
            }

            return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        }
        #endregion

        #region Search
        public ActionResult LogHit(string query, string language, Guid contentGuid)
        {
            var page = _contentRepository.Get<PageData>(contentGuid);

            if (page != null)
            {
                return Redirect(_urlResolver.GetUrl(page));
            }

            return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        }

        [HttpPost]
        public ActionResult Search(string searchString, string language)
        {
            var searchParams = new ElasticEpiSearchParams(searchString, language)
            {
                Query = searchString,
                IsExcludedFromStatistics = true,
                Take = 100
            };

            var result = ElasticEpiSearch<ElasticEpiDocument>.Current.Search(searchParams);

            var viewModel = new SearchResultViewModel();

            var norwegianCultures = ElasticEpiLanguageHelper.GetNorwegianCultures().Bokmal;

            foreach (var res in result.Items)
            {
                viewModel.Items.Add(new SearchResultViewModel.SearchResultItemViewModel
                {
                    Title = res.Document.Title,
                    Ingress = res.Document.Ingress,
                    Content = res.Document.Body,
                    Type = res.Document.TypeName,
                    ContentGuid = res.Document.ContentGuid,
                    Language = res.Document.LanguageName,
                    PublishDate = res.Document.StartPublish?.ToString(norwegianCultures.DateTimeFormat.ShortDatePattern),
                    Score = res.Score.ToString("G")
                });
            }

            var converted = JsonConvert.SerializeObject(viewModel, Formatting.None, new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            });

            return Json(converted, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult UpdateEngine()
        {
            try
            {
                ElasticEpiSearchTemplate<ElasticEpiDocument>.Rebuild();
                ServiceLocator.Current.GetInstance<IElasticIndexer<ElasticEpiDocument>>().UpdateEditorSynonyms();
            }
            catch (Exception)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            return new HttpStatusCodeResult(HttpStatusCode.OK);
        }
        #endregion

        #region Initialization
        private static string ResolveViewPath(string viewName)
        {
            return Paths.ToClientResource(typeof(AdministrationToolController),
                $"Views/{RoutesInitialization.ControllerName}/{viewName}.cshtml");
        }

        private AdministrationToolViewModel ResolveViewModel()
        {
            var viewModel = new AdministrationToolViewModel();

            foreach (var language in ServiceLocator.Current.GetInstance<ILanguageBranchRepository>().ListEnabled())
            {
                viewModel.AvailableLanguages.Add(new AdministrationToolViewModel.LanguageViewModel
                {
                    Name = language.Name,
                    Code = language.LanguageID
                });
            }

            ResolveBoosts(viewModel);
            ResolveDecays(viewModel);
            ResolveSynonymContainers(viewModel);
            ResolveBestBets(viewModel);
            ResolveAllContent(viewModel);
            ResolvePropertyWeights(viewModel);


            return viewModel;
        }

        private void ResolvePropertyWeights(AdministrationToolViewModel viewModel)
        {
            var previous = _weightSettingRepository.ReadAll();
            var current = ServiceLocator.Current.GetInstance<ElasticEpiDocument>()
                .GetLanguageAnalyzableProperties().Select(p => p.ToElasticPropertyName()).ToList();

            foreach (var propertyName in current)
            {
                _weightSettingRepository.CreateIfNotExisting(new WeightSetting(propertyName, 1),
                    pw => pw.Property == propertyName);
            }

            if (previous.Count != current.Count)
            {
                foreach (var property in previous.Where(ws => !current.Contains(ws.Property)))
                {
                    _weightSettingRepository.Delete(property);
                }
            }

            viewModel.PropertyWeightSettings = _weightSettingRepository.ReadAll();
        }

        private void ResolveAllContent(AdministrationToolViewModel viewModel)
        {
            foreach (var contentLink in ServiceLocator.Current.GetInstance<IContentLoader>().GetDescendents(ContentReference.StartPage).DistinctBy(c => c.ID))
            {
                var localizedContent =
                    ServiceLocator.Current.GetInstance<IContentLoader>()
                        .Get<IContent>(contentLink) as ILocalizable;

                if (localizedContent != null)
                {
                    foreach (var language in localizedContent.ExistingLanguages)
                    {
                        var lang = language.Name;

                        var content = ServiceLocator.Current.GetInstance<IContentLoader>().Get<IContent>(contentLink, language);

                        if (!viewModel.Content.ContainsKey(lang))
                        {
                            viewModel.Content[lang] = new List<AdministrationToolViewModel.ContentViewModel>();
                        }

                        viewModel.Content[lang].Add(new AdministrationToolViewModel.ContentViewModel
                        {
                            ContentGuid = content.ContentGuid,
                            Name = content.Name,
                            Language = lang
                        });
                    }
                }
            }
        }

        private void ResolveBestBets(AdministrationToolViewModel viewModel)
        {
            foreach (var bestBet in _bestBetRepository.ReadAll())
            {
                if (!viewModel.BestBets.ContainsKey(bestBet.Language))
                {
                    viewModel.BestBets[bestBet.Language] = new List<AdministrationToolViewModel.BestBetViewModel>();
                }

                viewModel.BestBets[bestBet.Language].Add(new AdministrationToolViewModel.BestBetViewModel
                {
                    Model = bestBet,
                    Contents =
                        bestBet.Contents.Select(
                            i =>
                                new AdministrationToolViewModel.BestBetContentViewModel
                                {
                                    Model = i,
                                    Name = ResolveContentName(i.ContentGuid, bestBet.Language)
                                }).OrderByDescending(i => i.Model.Order).ToList()
                });
            }
        }

        private void ResolveSynonymContainers(AdministrationToolViewModel viewModel)
        {
            foreach (var synonymContainer in _synonymContainerRepository.ReadAll())
            {
                if (!viewModel.SynonymContainers.ContainsKey(synonymContainer.Language))
                {
                    viewModel.SynonymContainers[synonymContainer.Language] = new List<SynonymContainer>();
                }

                viewModel.SynonymContainers[synonymContainer.Language].Add(synonymContainer);
            }
        }

        private void ResolveDecays(AdministrationToolViewModel viewModel)
        {
            var cleanup = new List<Guid>();

            foreach (var child in _contentLoader.Get<PageData>(ContentReference.StartPage).GetDescendants(2))
            {
                if (child is ISearchConfigurableParent)
                {
                    var decay = new Decay(child.ContentGuid);
                    cleanup.Add(child.ContentGuid);

                    _decayRepository.CreateIfNotExisting(decay, m => m.ContentGuid == decay.ContentGuid);
                }
            }

            foreach (var decay in _decayRepository.ReadAll())
            {
                if (!cleanup.Contains(decay.ContentGuid))
                {
                    _decayRepository.Delete(decay);
                    continue;
                }

                viewModel.Decays.Add(new AdministrationToolViewModel.DecayViewModel
                {
                    Model = decay,
                    Name = ResolveContentName(decay.ContentGuid),
                    Breadcrumb = ResolveBreadcrumb(decay.ContentGuid)
                });
            }
        }

        private void ResolveBoosts(AdministrationToolViewModel viewModel)
        {
            var cleanup = new List<Guid>();

            foreach (var child in _contentLoader.Get<PageData>(ContentReference.StartPage).GetDescendants(2))
            {
                if (child is ISearchConfigurableParent)
                {
                    var boost = new Boost(child.ContentGuid);
                    cleanup.Add(child.ContentGuid);

                    _boostRepository.CreateIfNotExisting(boost, i => i.ContentGuid == boost.ContentGuid);
                }
            }

            foreach (var boost in _boostRepository.ReadAll())
            {
                if (!cleanup.Contains(boost.ContentGuid))
                {
                    _boostRepository.Delete(boost);
                    continue;
                }

                viewModel.Boosts.Add(new AdministrationToolViewModel.BoostViewModel
                {
                    Model = boost,
                    Name = ResolveContentName(boost.ContentGuid),
                    Breadcrumb = ResolveBreadcrumb(boost.ContentGuid)
                });
            }
        }
        #endregion

        #region Misc helpers
        private string ResolveBreadcrumb(Guid contentGuid)
        {
            var content = _contentRepository.Get<IContent>(contentGuid);

            return content != null ? BreadcrumbsHelper.FromContentAsNames(content) : string.Empty;
        }

        private string ResolveContentName(Guid contentGuid, string language = null)
        {
            IContent content;

            if (language == null)
            {
                content = _contentRepository.Get<IContent>(contentGuid);
            }
            else
            {
                var localizedContent =
                    ServiceLocator.Current.GetInstance<IContentLoader>()
                        .Get<IContent>(contentGuid) as ILocalizable;

                var culture = localizedContent?.ExistingLanguages.FirstOrDefault(i => i.Name == language);

                content = _contentRepository.Get<IContent>(contentGuid, culture);
            }

            return content != null ? content.Name : string.Empty;
        }
        #endregion

    }
}