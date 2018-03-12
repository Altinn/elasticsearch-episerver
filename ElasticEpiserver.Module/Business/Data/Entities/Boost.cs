using System;
using EPiServer.Data.Dynamic;

namespace ElasticEpiserver.Module.Business.Data.Entities
{
    [EPiServerDataStore(StoreName = "ElasticEpiserverBoostDDS", AutomaticallyCreateStore = true, AutomaticallyRemapStore = true)]
    public class Boost : DynamicDataBase
    {
        public Guid ContentGuid { get; set; }
        public double Value { get; set; }

        public Boost()
        {
            // Required by EPiServer Dynamic Data Store
        }

        public Boost(Guid contentGuid)
        {
            ContentGuid = contentGuid;
            Value = 1;
        }
    }
}