using EPiServer.Core;

namespace ElasticEpiserver.Module.Engine.Search
{
    /// <summary>
    /// Implement this to specify how the best bet validation should work.
    /// </summary>
    public interface IBestBetValidator
    {
        /// <summary>
        /// Determines whether the given Episerver page should be included in the search result. 
        /// </summary>
        /// <param name="page">The Episerver page.</param>
        /// <param name="options">The best bet options object to use during validation.</param>
        /// <returns></returns>
        bool ShouldIncludeAsBestBet(PageData page, BestBetOptions options);
    }
}
