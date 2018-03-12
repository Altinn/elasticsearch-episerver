using ElasticEpiserver.Module.Engine.Indexing;
using EPiServer;
using EPiServer.Core;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.ServiceLocation;
using EPiServer.Web;

namespace ElasticEpiserver.Module.Business.Events
{
    [InitializableModule]
    public class ContentEventListener : IInitializableModule
    {
        public Injected<IContentEvents> ContentEvents { get; set; }
        public Injected<IElasticIndexer<ElasticEpiDocument>> Indexer { get; set; }

        public void Initialize(InitializationEngine context)
        {
            ContentEvents.Service.PublishedContent += ServiceOnPublishedContent;
            ContentEvents.Service.DeletedContent += ServiceOnDeletedContent;
            ContentEvents.Service.DeletedContentLanguage += ServiceOnDeletedContentLanguage;
            ContentEvents.Service.MovedContent += ServiceOnMovedContent;
        }

        private void ServiceOnMovedContent(object sender, ContentEventArgs contentEventArgs)
        {
            try
            {
                if (contentEventArgs.TargetLink.CompareToIgnoreWorkID(SiteDefinition.Current.WasteBasket))
                {
                    Indexer.Service.OnContentDeleted(contentEventArgs.Content);
                }
            }
            catch
            {
                // ignored
            }
        }

        private void ServiceOnDeletedContentLanguage(object sender, ContentEventArgs contentEventArgs)
        {
            try
            {
                Indexer.Service.OnContentLanguageDeleted(contentEventArgs.Content);
            }
            catch
            {
                // ignored
            }

        }

        private void ServiceOnDeletedContent(object sender, DeleteContentEventArgs deleteContentEventArgs)
        {
            try
            {
                Indexer.Service.OnContentDeleted(deleteContentEventArgs.Content);
            }
            catch
            {
                // ignored
            }

        }

        private void ServiceOnPublishedContent(object sender, ContentEventArgs contentEventArgs)
        {
            try
            {
                Indexer.Service.OnContentPublished(contentEventArgs.Content);
            }
            catch
            {
                // ignored
            }

        }

        public void Uninitialize(InitializationEngine context)
        {
            ContentEvents.Service.PublishedContent -= ServiceOnPublishedContent;
            ContentEvents.Service.DeletedContent -= ServiceOnDeletedContent;
            ContentEvents.Service.DeletedContentLanguage -= ServiceOnDeletedContentLanguage;
            ContentEvents.Service.MovedContent -= ServiceOnMovedContent;
        }
    }
}