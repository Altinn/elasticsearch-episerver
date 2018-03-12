using System;
using System.Reflection;
using ElasticEpiserver.Module.Business.Data;
using ElasticEpiserver.Module.Engine.Indexing;
using ElasticEpiserver.Module.Engine.Search;
using EPiServer;
using EPiServer.Core;
using EPiServer.ServiceLocation;

namespace ElasticEpiserver.Module.Engine.Parsing.Pages
{
    public abstract class PageParserBase<T, TK> where T : PageData where TK : ElasticEpiDocument, new()
    {
        public virtual TK Parse(T page)
        {
            var baseObj = new ElasticEpiDocument
            {
                Title = page.Name,
                StartPublish = page.StartPublish,
                StopPublish = page.StopPublish,
                ContentGuid = page.ContentGuid.ToString(),
                LanguageName = page.Language?.Name,
                TypeName = page.GetOriginalType().Name,
                ContainerContentGuid = ResolveContainerContentGuid(page),
                Breadcrumbs = BreadcrumbsHelper.FromContent(page)
            };

            var derivedObj = new TK();

            PopulateCommonProperties(baseObj.GetType(), baseObj, derivedObj);

            return derivedObj;
        }

        private static void PopulateCommonProperties(IReflect type, ElasticEpiDocument source, TK destination)
        {
            var fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

            foreach (var fi in fields)
            {
                fi.SetValue(destination, fi.GetValue(source));
            }
        }

        private static string ResolveContainerContentGuid(PageData page)
        {
            try
            {
                if (page is ISearchConfigurableParent)
                {
                    return page.ContentGuid.ToString();
                }
                if (page.ParentLink != null)
                {
                    return ResolveContainerContentGuid(ServiceLocator.Current.GetInstance<IContentLoader>().Get<PageData>(page.ParentLink));
                }
            }
            catch (Exception)
            {
                return null;
            }

            return null;
        }
    }
}