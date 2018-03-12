using System;
using System.Globalization;
using EPiServer;
using EPiServer.Core;
using EPiServer.ServiceLocation;

namespace ElasticEpiserver.Module.Business.Helpers
{
    public class PageResolver
    {
        public static PageData ResolvePage(Guid contentGuid, string languageName = null)
        {
            try
            {
                if (languageName == null)
                {
                    return ServiceLocator.Current.GetInstance<IContentLoader>().Get<IContent>(contentGuid) as PageData;
                }

                return ServiceLocator.Current.GetInstance<IContentLoader>()
                    .Get<IContent>(contentGuid, CultureInfo.GetCultureInfo(languageName)) as PageData;
            }
            catch (ContentNotFoundException)
            {
            }

            return null;
        }
    }
}
