using System;
using System.Collections.Generic;

namespace Wale.Localization
{
    public static class Interpreter
    {
        public static string Locale { get; set; } = "English";
        public static WaleLocalization Current => GetLocalization(Locale);
        public static IEnumerable<string> Locales => List.Keys;

        private static WaleLocalization GetLocalization(string locale)
        {
            if (List.ContainsKey(locale)) return List[locale];
            else throw new NullReferenceException("There is no such Locale");
        }

        private static readonly Dictionary<string, WaleLocalization> List = new Dictionary<string, WaleLocalization>() {
            { "English", new WaleEnglish() }
        };
    }
}
