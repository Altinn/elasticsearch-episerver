using System.Globalization;
using System.Text;
using ElasticEpiserver.Module.Engine.Parsing.Blocks;
using EPiServer;
using EPiServer.Core;
using EPiServer.Core.Html;
using EPiServer.Core.Html.StringParsing;
using EPiServer.Logging;
using EPiServer.Security;
using EPiServer.ServiceLocation;

namespace ElasticEpiserver.Module.Engine.Parsing
{
    public interface IEpiContentParser
    {
        string ParseToString(CultureInfo language, params XhtmlString[] content);
        string ParseContentArea(CultureInfo language, ContentArea content);
    }

    /// <summary>
    /// Processing of XHTML and ContentAreas
    /// </summary>
    [ServiceConfiguration(ServiceType = typeof(IEpiContentParser), Lifecycle = ServiceInstanceScope.Singleton)]
    public class EpiContentParser : IEpiContentParser
    {
        private readonly ILogger _logger = LogManager.GetLogger(typeof(EpiContentParser));

        /// <summary>
        /// Process a ContentArea to the contained string value - Handles the loading of blockvalues within the contentArea, replacing it with the "rendered" string. 
        /// </summary>
        /// <param name="language"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public string ParseContentArea(CultureInfo language, ContentArea content)
        {
            return ParseToString(language, content);
        }

        /// <summary>
        /// Process XHTMLString to the contained string value - Handles the loading of blockvalues within the XhtmlString, replacing it with the "rendered" string from the block. 
        /// </summary>
        /// <param name="language"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public string ParseToString(CultureInfo language, params XhtmlString[] content)
        {
            var sb = new StringBuilder();

            foreach (var xhtmlString in content)
            {
                if (xhtmlString == null)
                {
                    continue;
                }

                foreach (var fragment in xhtmlString.Fragments.GetFilteredFragments(PrincipalInfo.AnonymousPrincipal))
                {
                    if (fragment is ContentFragment)
                    {
                        var contentFragment = fragment as ContentFragment;

                        var block = ServiceLocator.Current.GetInstance<IContentLoader>().Get<IContent>(contentFragment.ContentGuid, language) as BlockData;

                        if (block != null)
                        {
                            var parsed = ServiceLocator.Current.GetInstance<IEpiBlockParser>().Parse(language, block);

                            if (!string.IsNullOrWhiteSpace(parsed))
                            {
                                sb.Append(parsed);
                                sb.Append(" ");
                            }
                            else
                            {
                                _logger.Debug($"{block.GetType().BaseType?.Name} has no block parsing strategy");
                            }

                        }
                    }
                    else if (fragment is StaticFragment)
                    {
                        sb.Append((fragment as StaticFragment).InternalFormat);
                    }
                }
            }

            return TextIndexer.StripHtml(sb.ToString().Trim(), 0);
        }
    }
}