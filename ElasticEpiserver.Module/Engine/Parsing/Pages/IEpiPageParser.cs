using ElasticEpiserver.Module.Engine.Indexing;
using EPiServer.Core;

namespace ElasticEpiserver.Module.Engine.Parsing.Pages
{
    public interface IEpiPageParser
    {
        ElasticEpiDocument Parse(PageData page);
    }
}
