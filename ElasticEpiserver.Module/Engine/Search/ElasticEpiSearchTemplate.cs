using System;
using System.Collections.Generic;
using System.Linq;
using ElasticEpiserver.Module.Business.Data;
using ElasticEpiserver.Module.Business.Data.Entities;
using ElasticEpiserver.Module.Business.Extensions;
using ElasticEpiserver.Module.Engine.Client;
using ElasticEpiserver.Module.Engine.Indexing;
using Elasticsearch.Net;
using EPiServer.ServiceLocation;
using Nest;
using Newtonsoft.Json.Linq;

namespace ElasticEpiserver.Module.Engine.Search
{
    public static class ElasticEpiSearchTemplate<T> where T : ElasticEpiDocument, new()
    {
        public const string Name = "epi_search_template";
        public const string ParamQuery = "query";
        public const string ParamExcludedTypeNames = "excludedTypes";
        public const string ParamIncludedTypeNames = "includedTypes";
        public const string ParamExcludedIds = "excludedIds";
        public const string ParamSkip = "from"; // Do not change (ELS reserved)
        public const string ParamTake = "size"; // Do not change (ELS reserved)

        public static void Rebuild()
        {
            var request = new SearchRequest
            {
                Query = new FunctionScoreQuery
                {
                    Functions = GetQueryFunctions(),
                    ScoreMode = FunctionScoreMode.Multiply,
                    Query = new BoolQuery
                    {
                        Filter = GetQueryFilters(),
                        Must = new List<QueryContainer>
                        {
                            new MultiMatchQuery
                            {
                                Type = TextQueryType.MostFields,
                                MinimumShouldMatch = MinimumShouldMatch.Percentage(80),
                                Query = $"{{{{{ParamQuery}}}}}",
                                Fields = GetQueryFields()
                            }
                        },
                        MustNot = new List<QueryContainer>
                        {
                            // Exclude unpublished documents
                            new DateRangeQuery
                            {
                                LessThanOrEqualTo = DateMath.Now,
                                Field = Infer.Field<ElasticEpiDocument>(e => e.StopPublish)
                            },
                            // Excluded types
                            new MatchQuery
                            {
                                Field = Infer.Field<ElasticEpiDocument>(e => e.TypeName),
                                Query = $"{{{{{ParamExcludedTypeNames}}}}}",
                                ZeroTermsQuery = ZeroTermsQuery.None
                            },
                            // Excluded ids
                            new MatchQuery
                            {
                                Field = Infer.Field<ElasticEpiDocument>(e => e.ContentGuid),
                                Query = $"{{{{{ParamExcludedIds}}}}}",
                                ZeroTermsQuery = ZeroTermsQuery.None
                            }
                        }
                    }
                }
            };

            var requestString = ElasticEpiClient.Current.Get().Serializer.SerializeToString(request);
            var jsonObject = JObject.Parse(requestString);

            jsonObject.Add(ParamSkip, "{{from}}{{^from}}0{{/from}}");
            jsonObject.Add(ParamTake, "{{size}}{{^size}}10{{/size}}");

            requestString = ElasticEpiClient.Current.Get().Serializer.SerializeToString(jsonObject);

            ElasticEpiClient.Current.Get()
                .PutSearchTemplate(new PutSearchTemplateDescriptor(Name).Template(requestString));
        }

        private static IList<QueryContainer> GetQueryFilters()
        {
            var filters = new List<QueryContainer>
            {
                // Included types
                new MatchQuery
                {
                    Field = Infer.Field<ElasticEpiDocument>(e => e.TypeName),
                    Query = $"{{{{{ParamIncludedTypeNames}}}}}",
                    ZeroTermsQuery = ZeroTermsQuery.All
                }
            };

            foreach (var filter in ServiceLocator.Current.GetInstance<ElasticEpiDocument>().GetMatchFilterProperties())
            {
                var propertyName = filter.ToElasticPropertyName();

                // Adds user specified match filters
                filters.Add(new MatchQuery
                {
                    Field = propertyName,
                    Query = $"{{{{{propertyName}}}}}",
                    ZeroTermsQuery = ZeroTermsQuery.All
                });
            }

            return filters;
        }

        private static IList<IScoreFunction> GetQueryFunctions()
        {
            var functions = new List<IScoreFunction>();

            functions.Add(GetHitBoostFunction());
            //functions.AddRange(GetSimulatedDecayFunctions());
            functions.AddRange(GetDecayFunctions());
            functions.AddRange(GetContentBoostingFunctions());

            return functions;
        }

        private static FieldValueFactorFunction GetHitBoostFunction()
        {
            // only apply hit boosting to documents with 10 or more clicks
            var filter = new NumericRangeQuery
            {
                Field = Infer.Field<ElasticEpiDocument>(e => e.NumClicks),
                GreaterThanOrEqualTo = 10
            };

            return new FieldValueFactorFunction
            {
                Field = Infer.Field<ElasticEpiDocument>(e => e.NumClicks),
                Modifier = FieldValueFactorModifier.Log2P,
                Factor = 1,
                Filter = filter
            };
        }

        private static IEnumerable<IScoreFunction> GetSimulatedDecayFunctions()
        {
            foreach (var decay in ServiceLocator.Current.GetInstance<IDynamicDataRepository<Decay>>().ReadAll().Where(e => e.IsActive))
            {
                var filter = new BoolQuery
                {
                    Must = new List<QueryContainer>
                    {
                        new MatchQuery
                        {
                            Field = Infer.Field<ElasticEpiDocument>(e => e.ContainerContentGuid),
                            Query = decay.ContentGuid.ToString()
                        },
                        // only apply simulated decay to documents older than 2 years
                        new DateRangeQuery
                        {
                            Field = Infer.Field<ElasticEpiDocument>(e => e.StartPublish),
                            LessThanOrEqualTo = DateMath.Now.Subtract(TimeSpan.FromDays(365*2))
                        }
                    }
                };

                yield return new WeightFunction
                {
                    Weight = 0.01,
                    Filter = filter
                };
            }
        }

        private static IEnumerable<IScoreFunction> GetDecayFunctions()
        {
            foreach (var decay in ServiceLocator.Current.GetInstance<IDynamicDataRepository<Decay>>().ReadAll().Where(e => e.IsActive))
            {
                var filter = new BoolQuery
                {
                    Must = new List<QueryContainer>
                    {
                        new MatchQuery
                        {
                            Field = Infer.Field<ElasticEpiDocument>(e => e.ContainerContentGuid),
                            Query = decay.ContentGuid.ToString()
                        },
                        // only apply decay to documents newer than 2 years
                        //new DateRangeQuery
                        //{
                        //    Field = Infer.Field<ElasticEpiDocument>(e => e.StartPublish),
                        //    GreaterThan = DateMath.Now.Subtract(TimeSpan.FromDays(365*2))
                        //}
                    }
                };

                yield return new GaussDateDecayFunction
                {
                    Field = Infer.Field<ElasticEpiDocument>(e => e.StartPublish),
                    Scale = $"{decay.Scale}d",
                    Decay = decay.Rate,
                    Offset = $"{decay.Offset}d",
                    Filter = filter
                };
            }
        }

        private static IEnumerable<IScoreFunction> GetContentBoostingFunctions()
        {
            foreach (var boost in ServiceLocator.Current.GetInstance<IDynamicDataRepository<Boost>>().ReadAll())
            {
                yield return new WeightFunction
                {
                    Filter = new MatchQuery
                    {
                        Field = Infer.Field<ElasticEpiDocument>(e => e.ContainerContentGuid),
                        Query = boost.ContentGuid.ToString()
                    },

                    Weight = boost.Value
                };
            }
        }

        private static Fields GetQueryFields()
        {
            var fieldWeights = new List<string>();
            var properties = new List<string>();

            foreach (var field in ServiceLocator.Current.GetInstance<IDynamicDataRepository<WeightSetting>>().ReadAll())
            {
                var elasticPropertyName = field.Property.ToElasticPropertyName();

                fieldWeights.Add($"{elasticPropertyName}.{IndexSettingsFactory.Names.Analyzers.Shorthand.Language}^{field.Weight:0.#}");
                fieldWeights.Add($"{elasticPropertyName}.{IndexSettingsFactory.Names.Analyzers.Shorthand.Ngram}^{field.Weight / 3:0.#}");

                properties.Add(elasticPropertyName);
            }

            foreach (var property in ServiceLocator.Current.GetInstance<ElasticEpiDocument>().GetLanguageAnalyzableProperties())
            {
                var elasticPropertyName = property.ToElasticPropertyName();

                if (properties.Contains(elasticPropertyName))
                {
                    continue;
                }

                // Add default weighting if not set by admin through Epi DDS
                fieldWeights.Add($"{elasticPropertyName}.{IndexSettingsFactory.Names.Analyzers.Shorthand.Language}^1.0");
                fieldWeights.Add($"{elasticPropertyName}.{IndexSettingsFactory.Names.Analyzers.Shorthand.Ngram}^0.3");
            }

            return Infer.Fields(fieldWeights.ToArray());
        }

    }
}