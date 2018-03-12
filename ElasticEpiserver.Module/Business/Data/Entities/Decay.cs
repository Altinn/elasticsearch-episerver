using System;
using EPiServer.Data.Dynamic;

namespace ElasticEpiserver.Module.Business.Data.Entities
{
    [EPiServerDataStore(StoreName = "ElasticEpiserverDecayDDS", AutomaticallyCreateStore = true, AutomaticallyRemapStore = true)]
    public class Decay : DynamicDataBase
    {
        public Guid ContentGuid { get; set; }
        public double Rate { get; set; } = 0.5;
        public int Offset { get; set; } = 60;
        public int Scale { get; set; } = 365;
        public double Weight { get; set; } = 1.0;
        public bool IsActive { get; set; }

        public Decay()
        {
            Initialize();
        }

        public Decay(Guid contentGuid)
        {
            Initialize();
            ContentGuid = contentGuid;
        }

        private void Initialize()
        {
            Rate = 0.5;
            Offset = 60;
            Scale = 365;
            Weight = 1.0;
            IsActive = false;
        }
    }
}