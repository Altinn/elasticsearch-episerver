using System;

namespace ElasticEpiserver.Module.Engine.Models
{
    public class DocumentClickCount
    {
        public Guid ContentGuid { get; set; }
        public string Language { get; set; }
        public int Count { get; set; }
    }
}