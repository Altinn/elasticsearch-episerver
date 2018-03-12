using System.Collections.Generic;
using ElasticEpiserver.Module.Engine.Indexing;

namespace ElasticEpiserver.Module.Engine.Search
{
    public class ElasticEpiSearchParams
    {
        /// <summary>
        /// The search query.
        /// </summary>
        public string Query { get; set; }

        /// <summary>
        /// The language in which you wish to perform the search.
        /// </summary>
        public string LanguageName { get; set; }

        /// <summary>
        /// The number of documents in the search result to skip.
        /// </summary>
        public int Skip { get; set; }

        /// <summary>
        /// The number of documents in the search result to take.
        /// </summary>
        public int Take { get; set; }

        /// <summary>
        /// Names of specific types to include in the search result.
        /// </summary>
        public List<string> IncludedTypes { get; set; }

        /// <summary>
        /// Names of specific types to exclude from the search result.
        /// </summary>
        public List<string> ExcludedTypes { get; set; }

        /// <summary>
        /// Specify pairs of property names and values on which to perform an exact match query. 
        /// These property names must be returned by your index document's override of 
        /// <seealso cref="ElasticEpiDocument.GetMatchFilterProperties"/>.
        /// </summary>
        public Dictionary<string, string> MatchFilters { get; set; }

        /// <summary>
        /// Specifies whether to include the search in the statistics index.
        /// </summary>
        public bool IsExcludedFromStatistics { get; set; }

        /// <summary>
        /// Specifies how to handle best bets in the search
        /// </summary>
        public BestBetOptions BestBetOptions { get; set; }

        public ElasticEpiSearchParams(string query, string languageName)
        {
            Query = query;
            LanguageName = languageName;

            IncludedTypes = new List<string>();
            ExcludedTypes = new List<string>();
            MatchFilters = new Dictionary<string, string>();

            Skip = 0;
            Take = 10;

            BestBetOptions = new BestBetOptions();
        }
    }

    /// <summary>
    /// Class for specifying best bet options.
    /// </summary>
    public class BestBetOptions
    {
        /// <summary>
        /// Validator implementation.
        /// </summary>
        public IBestBetValidator Validator { get; set; }
        /// <summary>
        /// Use this to send data with the best bet options.
        /// This will be sent back to the validator upon validation.
        /// </summary>
        public object Data { get; set; }
    }
}