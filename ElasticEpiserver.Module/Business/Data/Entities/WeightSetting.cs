using EPiServer.Data.Dynamic;

namespace ElasticEpiserver.Module.Business.Data.Entities
{
    [EPiServerDataStore(StoreName = "ElasticEpiserverWeightSettingDDS", AutomaticallyCreateStore = true, AutomaticallyRemapStore = true)]
    public class WeightSetting : DynamicDataBase
    {
        public string Property { get; set; }
        public double Weight { get; set; } = 1;

        public WeightSetting()
        {
            // Required by EPiServer Dynamic Data Store
        }

        public WeightSetting(string property, double weight)
        {
            Weight = weight;
            Property = property;
        }
    }
}