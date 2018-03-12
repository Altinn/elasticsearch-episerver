using System;
using EPiServer.Core;

namespace ElasticEpiserver.Module.Engine.Indexing
{
    public interface IElasticIndexer<T> where T : ElasticEpiDocument, new()
    {
        void RebuildIndex(Action<string> statusMessageCallback);
        void OnContentPublished(IContent content);
        void OnContentDeleted(IContent content);
        void OnContentLanguageDeleted(IContent content);
        bool UpdateClickCountOnDocuments();
        void UpdateEditorSynonyms();
        void IndexSearchRequestForStatistics(string query, string language, long totalHits, long time);
        void IndexHitLogDocument(string query, string language, Guid contentGuid);
    }
}
