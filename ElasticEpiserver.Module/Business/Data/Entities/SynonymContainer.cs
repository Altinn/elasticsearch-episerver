using System.Collections.Generic;
using EPiServer.Data.Dynamic;

namespace ElasticEpiserver.Module.Business.Data.Entities
{
    [EPiServerDataStore(StoreName = "ElasticEpiserverSynonymContainerDDS", AutomaticallyCreateStore = true, AutomaticallyRemapStore = true)]
    public class SynonymContainer : DynamicDataBase
    {
        public string Language { get; set; }
        public string Word { get; set; }
        public IList<string> Synonyms { get; set; }

        public SynonymContainer()
        {
            Initialize();
        }

        public SynonymContainer(string word, string language)
        {
            Initialize();
            Word = word;
            Language = language;
        }

        private void Initialize()
        {
            Synonyms = new List<string>();
        }
    }
}