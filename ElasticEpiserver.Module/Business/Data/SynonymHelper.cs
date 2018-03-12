using System;
using System.Collections.Generic;
using System.Linq;
using ElasticEpiserver.Module.Business.Data.Entities;
using EPiServer.ServiceLocation;

namespace ElasticEpiserver.Module.Business.Data
{
    public static class SynonymHelper
    {
        public static string[] ResolveSynonymsForLanguage(string language)
        {
            var resolved = new List<string>();

            foreach (var synonym in ServiceLocator.Current.GetInstance<IDynamicDataRepository<SynonymContainer>>()
                .ReadAll().Where(x => string.Equals(x.Language, language, StringComparison.CurrentCultureIgnoreCase)))
            {
                resolved.Add(synonym.Word + "," + string.Join(",", synonym.Synonyms));
            }

            if (resolved.Count <= 0)
            {
                resolved.Add("dummy,dummy");
            }

            return resolved.ToArray();
        }
    }
}