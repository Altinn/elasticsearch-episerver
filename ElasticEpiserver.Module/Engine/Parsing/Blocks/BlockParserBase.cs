using System.Globalization;
using EPiServer.Core;

namespace ElasticEpiserver.Module.Engine.Parsing.Blocks
{
    public abstract class BlockParserBase<T> where T : BlockData
    {
        public abstract string Parse(CultureInfo language, T block);
    }
}
