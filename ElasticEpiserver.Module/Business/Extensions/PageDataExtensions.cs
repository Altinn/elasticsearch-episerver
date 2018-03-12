using System.Collections.Generic;
using System.Linq;
using EPiServer;
using EPiServer.Core;
using EPiServer.ServiceLocation;

namespace ElasticEpiserver.Module.Business.Extensions
{
    /// <summary>
    /// Source:
    /// https://github.com/Geta/EPi.Extensions
    /// https://github.com/Geta/EPi.Extensions/blob/master/src/Geta.EPi.Extensions/PageDataExtensions.cs
    /// </summary>
    public static class PageDataExtensions
    {
        public static IEnumerable<T> GetChildren<T>(this PageData parentPage)
            where T : PageData
        {
            if (parentPage == null)
            {
                return Enumerable.Empty<T>();
            }
            var contentLoader = ServiceLocator.Current.GetInstance<IContentLoader>();
            return contentLoader.GetChildren<T>(parentPage.ContentLink);
        }

        public static IEnumerable<PageData> GetDescendants(this PageData pageData, int levels)
        {
            return pageData.GetDescendants<PageData>(levels);
        }

        public static IEnumerable<T> GetDescendants<T>(this PageData pageData, int levels) where T : PageData
        {
            if (pageData == null || levels <= 0)
            {
                yield break;
            }

            foreach (var child in pageData.GetChildren<T>())
            {
                yield return child;

                if (levels <= 1)
                {
                    continue;
                }

                foreach (var grandChild in child.GetDescendants<T>(levels - 1))
                {
                    yield return grandChild;
                }
            }
        }
    }
}
