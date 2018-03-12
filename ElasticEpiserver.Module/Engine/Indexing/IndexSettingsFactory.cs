using ElasticEpiserver.Module.Business.Data;
using ElasticEpiserver.Module.Engine.Languages;
using Nest;
using Language = ElasticEpiserver.Module.Engine.Languages.Language;

// ReSharper disable MemberHidesStaticFromOuterClass

namespace ElasticEpiserver.Module.Engine.Indexing
{
    public static class IndexSettingsFactory
    {
        public static class Names
        {
            public static class Analyzers
            {
                public const string Language = "epi_language_analyzer";
                public const string Ngram = "epi_ngram_analyzer";

                public static class Shorthand
                {
                    public const string Language = "language";
                    public const string Ngram = "ngram";
                }
            }

            public static class TokenFilters
            {
                public const string EditorSynonyms = "epi_editor_synonyms";
            }
        }

        public static IndexState GetByLanguageName(string languageName)
        {
            var language = ElasticEpiLanguageHelper.Resolve(languageName);

            switch (language)
            {
                case Language.English:
                    return GetEnglishState();
                case Language.Bokmal:
                case Language.Nynorsk:
                    return GetNorwegianState(language);
                default:
                    return GetEnglishState();
            }
        }

        private static IndexState GetEnglishState()
        {
            var settings = new IndexSettings
            {
                Analysis = new Analysis
                {
                    Tokenizers = new Tokenizers
                    {
                        {
                            "epi_ngram_tokenizer", new NGramTokenizer
                            {
                                MaxGram = 5,
                                MinGram = 3
                            }
                        }
                    },
                    TokenFilters = new TokenFilters
                    {
                        {
                            "epi_english_stopwords", new StopTokenFilter
                            {
                               StopWords = "_english_"
                            }
                        },
                        {
                            "epi_english_stemmer", new StemmerTokenFilter
                            {
                                Language ="english"
                            }
                        },
                        {
                            Names.TokenFilters.EditorSynonyms, new SynonymTokenFilter
                            {
                                IgnoreCase = true,
                                Synonyms = SynonymHelper.ResolveSynonymsForLanguage("en"),
                                Tokenizer = "keyword",
                                Expand = true
                            }
                        }

                    },
                    Analyzers = new Analyzers
                    {
                        {
                            Names.Analyzers.Ngram, new CustomAnalyzer
                            {
                                CharFilter = new[] {"html_strip"},
                                Tokenizer = "epi_ngram_tokenizer",
                                Filter = new[]
                                {
                                    "lowercase", "epi_english_stopwords", "epi_english_stemmer"
                                }
                            }
                        },
                        {
                            Names.Analyzers.Language, new CustomAnalyzer
                            {
                                CharFilter = new[] {"html_strip"},
                                Tokenizer = "standard",
                                Filter = new[]
                                {
                                    "lowercase", "epi_editor_synonyms", "epi_english_stopwords", "epi_english_stemmer"
                                }
                            }
                        }
                    }
                }
            };

            return new IndexState
            {
                Settings = settings
            };
        }

        private static IndexState GetNorwegianState(Language variant)
        {
            ElasticEpiLanguageHelper.NorwegianCulture norwegianCulture = ElasticEpiLanguageHelper.GetNorwegianCulture();

            var settings = new IndexSettings
            {
                Analysis = new Analysis
                {
                    Tokenizers = new Tokenizers
                    {
                        {
                            "epi_ngram_tokenizer", new NGramTokenizer
                            {
                                MaxGram = 5,
                                MinGram = 3
                            }
                        }
                    },
                    TokenFilters = new TokenFilters
                    {
                        {
                            "epi_norwegian_stopwords", new StopTokenFilter
                            {
                                StopWordsPath = "norwegian_stop.txt"
                            }
                        },
                        {
                            "epi_norwegian_stemmer", new StemmerTokenFilter
                            {
                                Language = variant == Language.Bokmal ? "light_norwegian" : "light_nynorsk"
                            }
                        },
                        {
                            "epi_norwegian_synonyms", new SynonymTokenFilter
                            {
                                SynonymsPath = "nynorsk.txt"
                            }
                        },
                        {
                            "epi_editor_synonyms", new SynonymTokenFilter
                            {
                                IgnoreCase = true,
                                Synonyms = SynonymHelper.ResolveSynonymsForLanguage(variant == Language.Bokmal ? norwegianCulture.Bokmal.Name : norwegianCulture.Nynorsk.Name),
                                Tokenizer = "keyword",
                                Expand = true
                            }
                        }

                    },
                    Analyzers = new Analyzers
                    {
                        {
                            Names.Analyzers.Ngram, new CustomAnalyzer
                            {
                                CharFilter = new[] {"html_strip"},
                                Tokenizer = "epi_ngram_tokenizer",
                                Filter = new[]
                                {
                                    "lowercase", "epi_norwegian_stopwords", "epi_norwegian_stemmer"
                                }
                            }
                        },
                        {
                            Names.Analyzers.Language, new CustomAnalyzer
                            {
                                CharFilter = new[] {"html_strip"},
                                Tokenizer = "standard",
                                Filter = new[]
                                {
                                    "lowercase", "epi_norwegian_synonyms", "epi_editor_synonyms", "epi_norwegian_stopwords", "epi_norwegian_stemmer"
                                }
                            }
                        }
                    }
                }
            };

            return new IndexState
            {
                Settings = settings
            };
        }
    }
}