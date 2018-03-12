using System;
using System.Collections.Generic;
using ElasticEpiserver.Module.Business.Data.Entities;
using ElasticEpiserver.Module.Engine.Client;

namespace ElasticEpiserver.Module.Models
{
    public class AdministrationToolViewModel
    {
        public IList<LanguageViewModel> AvailableLanguages { get; set; }
        public IList<DecayViewModel> Decays { get; set; }
        public IList<BoostViewModel> Boosts { get; set; }
        public IList<WeightSetting> PropertyWeightSettings { get; set; }

        public Dictionary<string, IList<SynonymContainer>> SynonymContainers { get; set; }
        public Dictionary<string, IList<BestBetViewModel>> BestBets { get; set; }
        public Dictionary<string, IList<ContentViewModel>> Content { get; set; }
        public string KibanaDashboardUrl { get; set; }

        public AdministrationToolViewModel()
        {
            Decays = new List<DecayViewModel>();
            Boosts = new List<BoostViewModel>();
            SynonymContainers = new Dictionary<string, IList<SynonymContainer>>();
            BestBets = new Dictionary<string, IList<BestBetViewModel>>();
            Content = new Dictionary<string, IList<ContentViewModel>>();
            KibanaDashboardUrl = ElasticEpiClient.Current.KibanaDashboardUrl;
            AvailableLanguages = new List<LanguageViewModel>();
            PropertyWeightSettings = new List<WeightSetting>();
        }

        public class BoostViewModel
        {
            public string Name { get; set; }
            public string Breadcrumb { get; set; }
            public Boost Model { get; set; }
        }

        public class DecayViewModel
        {
            public string Name { get; set; }
            public string Breadcrumb { get; set; }
            public Decay Model { get; set; }
        }

        public class BestBetViewModel
        {
            public BestBet Model { get; set; }
            public IList<BestBetContentViewModel> Contents { get; set; }
        }

        public class BestBetContentViewModel
        {
            public string Name { get; set; }
            public BestBetContent Model { get; set; }
        }

        public class ContentViewModel
        {
            public Guid ContentGuid { get; set; }
            public string Name { get; set; }
            public string Language { get; set; }
        }

        public class LanguageViewModel
        {
            public string Name { get; set; }
            public string Code { get; set; }
        }
    }
}