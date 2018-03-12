using System;

namespace ElasticEpiserver.Module.Engine.Indexing
{
    public class ElasticStatisticsDocument
    {
        public string Query { get; set; }
        public string Language { get; set; }
        public long ResponseTime { get; set; }
        public long NumberOfResults { get; set; }
        public DateTime SearchDate { get; set; }
    }
}