using ElasticEpiserver.Module.Engine.Indexing;
using EPiServer.PlugIn;
using EPiServer.Scheduler;
using EPiServer.ServiceLocation;

namespace ElasticEpiserver.Module.Business.Scheduler
{
    [ScheduledPlugIn(DisplayName = "ElasticEpiserver - Hit Boost Updater", SortIndex = 1)]
    public class HitBoostUpdaterScheduledJob : ScheduledJobBase
    {

        public override string Execute()
        {
            var ok = ServiceLocator.Current.GetInstance<IElasticIndexer<ElasticEpiDocument>>()
                .UpdateClickCountOnDocuments();

            return ok ? "OK" : "Failed or no documents found.";
        }
    }
}