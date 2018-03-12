using System.Globalization;
using System.Linq;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.ServiceLocation;

namespace ElasticEpiserver.Module.Engine.Languages
{
    public static class ElasticEpiLanguageHelper
    {
        private static readonly string[] BokmalLanguageCodes = { "nb", "nb-NO", "no" };
        private static readonly string[] NynorskLanguageCodes = { "nn", "no-nn", "nn-no" };

        public static Language Resolve(string languageName)
        {
            var language = languageName.ToLower();

            if (BokmalLanguageCodes.Contains(language))
            {
                return Language.Bokmal;
            }
            if (NynorskLanguageCodes.Contains(language))
            {
                return Language.Nynorsk;
            }

            return Language.English;
        }

        public static bool IsFallbackLanguageCandidate(ILocalizable content)
        {
            foreach (string nynorskLanguageCode in NynorskLanguageCodes)
            {
                if (content.ExistingLanguages.Contains(CultureInfo.GetCultureInfo(nynorskLanguageCode)))
                {
                    return false;
                }
            }

            // Return true if this content only exists in bokmål and not in nynorsk
            return HasNorwegianCulture(content);
        }

        private static bool HasNorwegianCulture(ILocalizable content)
        {
            foreach (string bokmalLanguageCode in BokmalLanguageCodes)
            {
                if (content.ExistingLanguages.Contains(CultureInfo.GetCultureInfo(bokmalLanguageCode)))
                {
                    return true;
                }
            }

            return false;
        }

        public static NorwegianCultures GetNorwegianCultures()
        {
            var norwegianCulture = new NorwegianCultures();

            var cultures = ServiceLocator.Current.GetInstance<ILanguageBranchRepository>().ListEnabled()
                .Select(x => x.Culture).ToList();

            foreach (string nynorskLanguageCode in NynorskLanguageCodes)
            {
                if (cultures.Contains(CultureInfo.GetCultureInfo(nynorskLanguageCode)))
                {
                    norwegianCulture.Nynorsk = CultureInfo.GetCultureInfo(nynorskLanguageCode);
                }
            }

            foreach (string bokmalLanguageCode in BokmalLanguageCodes)
            {
                if (cultures.Contains(CultureInfo.GetCultureInfo(bokmalLanguageCode)))
                {
                    norwegianCulture.Bokmal = CultureInfo.GetCultureInfo(bokmalLanguageCode);
                }
            }

            return norwegianCulture;
        }

        public class NorwegianCultures
        {
            public CultureInfo Nynorsk { get; set; }
            public CultureInfo Bokmal { get; set; }
        }
    }
}