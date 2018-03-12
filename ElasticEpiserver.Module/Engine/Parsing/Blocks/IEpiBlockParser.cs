using System.Globalization;
using EPiServer.Core;

namespace ElasticEpiserver.Module.Engine.Parsing.Blocks
{
    public interface IEpiBlockParser
    {
        string Parse(CultureInfo language, BlockData block);
    }
}
