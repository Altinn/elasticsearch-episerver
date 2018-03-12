using EPiServer.Core;

namespace ElasticEpiserver.Module.Engine.Indexing
{
    public interface IEpiContentValidator
    {
        bool ShouldBeIndexed(IContent content);
    }
}
