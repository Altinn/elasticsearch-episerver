using System.Collections.Generic;

namespace ElasticEpiserver.Module.Engine.Search
{
    public class ElasticEpiSearchResults
    {
        public ElasticEpiSearchResults()
        {
            Items = new List<ElasticEpiSearchResultItem>();
        }

        public IList<ElasticEpiSearchResultItem> Items { get; set; }
        public int Count { get; set; }
    }
}