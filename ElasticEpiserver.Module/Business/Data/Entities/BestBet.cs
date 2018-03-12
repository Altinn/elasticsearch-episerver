using System;
using System.Collections.Generic;
using EPiServer.Data.Dynamic;

namespace ElasticEpiserver.Module.Business.Data.Entities
{
    [EPiServerDataStore(StoreName = "ElasticEpiserverBestBetDDS", AutomaticallyCreateStore = true, AutomaticallyRemapStore = true)]
    public class BestBet : DynamicDataBase
    {
        public string Keyword { get; set; }
        public string Language { get; set; }
        public IList<BestBetContent> Contents { get; set; }

        public BestBet()
        {
            Initialize();
        }

        public BestBet(string keyword, string language)
        {
            Initialize();
            Keyword = keyword;
            Language = language;
        }

        private void Initialize()
        {
            Contents = new List<BestBetContent>();
        }
    }

    public class BestBetContent
    {
        public Guid ContentGuid { get; set; }
        public int Order { get; set; }
    }
}