using System;
using System.Collections.Generic;
using Nest;

namespace ElasticEpiserver.Module.Engine.Indexing
{
    [ElasticsearchType(IdProperty = nameof(ContentGuid))]
    public class ElasticEpiDocument
    {
        public string Title { get; set; }
        public string Ingress { get; set; }
        public string Body { get; set; }
        public string TypeName { get; set; }
        public string Breadcrumbs { get; set; }
        public DateTime? StartPublish { get; set; }
        public DateTime? StopPublish { get; set; }
        public string LanguageName { get; set; }
        public double NumClicks { get; set; }
        public string IsFallbackLanguage { get; set; }
        public string ContentGuid { get; set; }
        public string ContainerContentGuid { get; set; }

        public bool IsValid()
        {
            return !string.IsNullOrEmpty(Title) && LanguageName != null && ContentGuid != null && TypeName != null;
        }

        public virtual IList<string> GetLanguageAnalyzableProperties()
        {
            return new List<string> { nameof(Title), nameof(Ingress), nameof(Body) };
        }

        public virtual IList<string> GetMatchFilterProperties()
        {
            return new List<string> { nameof(IsFallbackLanguage) };
        }
    }
}