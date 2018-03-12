using System.Collections.Generic;

namespace ElasticEpiserver.Module.Models
{
    public class SearchResultViewModel
    {
        public IList<SearchResultItemViewModel> Items { get; set; }

        public SearchResultViewModel()
        {
            Items = new List<SearchResultItemViewModel>();
        }

        public class SearchResultItemViewModel
        {
            public string Title { get; set; }
            public string Ingress { get; set; }
            public string Content { get; set; }
            public string Type { get; set; }
            public string PublishDate { get; set; }
            public string Score { get; set; }
            public string ContentGuid { get; set; }
            public string Language { get; set; }
        }
    }
}