using ElasticEpiserver.Module.Engine.Indexing;

namespace ElasticEpiserver.Module.Engine.Search
{
    public class ElasticEpiSearchResultItem
    {
        public ElasticEpiDocument Document { get; set; }
        public double Score { get; set; }
    }
}