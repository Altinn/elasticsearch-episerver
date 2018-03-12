using System;
using Nest;

namespace ElasticEpiserver.Module.Engine.Client
{
    public class ElasticEpiClient
    {
        private ElasticClient _client;
        private string _elasticWebUrl;
        private string _baseIndexName;
        private string _hitsLogIndexName;
        private string _statisticsIndexName;
        private string _kibanaDashboardUrl;

        private ElasticEpiClient()
        {
        }

        private void Init()
        {
            var uri = new Uri(ElasticWebUrl);
            var settings = new ConnectionSettings(uri);

            _client = new ElasticClient(settings);
        }

        public ElasticClient Get()
        {
            if (_client == null)
                Init();

            return _client;
        }

        public string ContentIndexName(string languageName)
        {
            return $"{BaseIndexName}_{languageName}".ToLower();
        }

        public string BaseIndexName
        {
            get => _baseIndexName ?? "epi_index_content";
            set => _baseIndexName = value;
        }

        public string ElasticWebUrl
        {
            get => _elasticWebUrl ?? "http://localhost:9200";
            set => _elasticWebUrl = value;
        }

        public string HitsLogIndexName
        {
            get => _hitsLogIndexName ?? "epi_index_hitslog";
            set => _hitsLogIndexName = value;
        }

        public string StatisticsIndexName
        {
            get => _statisticsIndexName ?? "epi_index_statistics";
            set => _statisticsIndexName = value;
        }

        public string KibanaDashboardUrl
        {
            get => _kibanaDashboardUrl ?? "http://localhost:5601";
            set => _kibanaDashboardUrl = value;
        }

        private static ElasticEpiClient _instance;

        public static ElasticEpiClient Current => _instance ?? (_instance = new ElasticEpiClient());
    }
}