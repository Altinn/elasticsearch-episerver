using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer;
using EPiServer.Core;
using EPiServer.ServiceLocation;

namespace ElasticEpiserver.Module.Business.Data
{
    public static class BreadcrumbsHelper
    {
        public static string FromContent(IContent content)
        {
            try
            {
                var ids = GetAsContentReferences(content)
                    .Select(x => x.ID)
                    .ToList();

                return string.Join(",", ids);
            }
            catch (Exception)
            {
                // ignored
            }

            return null;
        }

        public static string FromContentAsNames(IContent content)
        {
            try
            {
                var names = GetAsContentReferences(content).Select(x => ServiceLocator.Current.GetInstance<IContentLoader>()
                    .Get<PageData>(x)).Select(p => p.Name).ToList();

                return string.Join(", ", names);
            }
            catch (Exception)
            {
                // ignored
            }

            return null;
        }

        private static IEnumerable<ContentReference> GetAsContentReferences(IContent content)
        {
            return ServiceLocator.Current.GetInstance<IContentLoader>()
                .GetAncestors(content.ContentLink)
                .Reverse()
                .SkipWhile(x => ContentReference.IsNullOrEmpty(x.ParentLink) || x.ContentLink == ContentReference.StartPage)
                .Select(x => x.ContentLink)
                .ToList();
        }
    }
}