using ElasticEpiserver.Module.Business.Data;
using ElasticEpiserver.Module.Business.Data.Entities;
using ElasticEpiserver.Module.Engine.Indexing;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.ServiceLocation;

namespace ElasticEpiserver.Module.Business.Initialization
{
    [ModuleDependency(typeof(ServiceContainerInitialization))]
    [InitializableModule]
    public class ElasticEpiserverInitialization : IConfigurableModule
    {
        public void Initialize(InitializationEngine context)
        {
        }

        public void Uninitialize(InitializationEngine context)
        {
        }

        public void ConfigureContainer(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton(typeof(IElasticIndexer<>), typeof(ElasticIndexer<>));
            context.Services.AddSingleton<IDynamicDataRepository<Decay>, DynamicDataRepository<Decay>>();
            context.Services.AddSingleton<IDynamicDataRepository<BestBet>, DynamicDataRepository<BestBet>>();
            context.Services.AddSingleton<IDynamicDataRepository<SynonymContainer>, DynamicDataRepository<SynonymContainer>>();
            context.Services.AddSingleton<IDynamicDataRepository<WeightSetting>, DynamicDataRepository<WeightSetting>>();
            context.Services.AddSingleton<IDynamicDataRepository<Boost>, DynamicDataRepository<Boost>>();
        }
    }
}