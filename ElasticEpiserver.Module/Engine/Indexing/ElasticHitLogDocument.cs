using System;

namespace ElasticEpiserver.Module.Engine.Indexing
{
    public class ElasticHitLogDocument
    {
        public string Query { get; set; }
        public Guid ContentGuid { get; set; }
        public string Language { get; set; }
        public DateTime SearchDate { get; set; } = DateTime.Now;
    }
}