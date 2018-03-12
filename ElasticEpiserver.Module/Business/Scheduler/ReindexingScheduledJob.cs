using ElasticEpiserver.Module.Engine.Indexing;
using EPiServer.PlugIn;
using EPiServer.Scheduler;
using EPiServer.ServiceLocation;

namespace ElasticEpiserver.Module.Business.Scheduler
{
    [ScheduledPlugIn(DisplayName = "ElasticEpiserver - Reindexing", SortIndex = 0)]
    public class ReindexingScheduledJob : ScheduledJobBase
    {
        private bool StopSignaled { get; set; }

        public ReindexingScheduledJob()
        {
            IsStoppable = true;
        }
        public override void Stop()
        {
            if (!IsStoppable)
                return;
            base.Stop();
            StopSignaled = true;
        }

        public bool IsStopSignaled()
        {
            return StopSignaled;
        }

        public override string Execute()
        {
            ServiceLocator.Current.GetInstance<IElasticIndexer<ElasticEpiDocument>>()
                .RebuildIndex(OnStatusChanged);

            return "OK";
        }
    }
}