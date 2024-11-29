using System.Collections.Generic;

namespace ExpertExplorer
{
    internal static class LocalizationHelper
    {
        private static readonly List<string> languages = new List<string>()
        {
            "English",
            "Swedish",
            "French",
            "Italian",
            "German",
            "Spanish",
            "Russian",
            "Romanian",
            "Bulgarian",
            "Macedonian",
            "Finnish",
            "Danish",
            "Norwegian",
            "Icelandic",
            "Turkish",
            "Lithuanian",
            "Czech",
            "Hungarian",
            "Slovak",
            "Polish",
            "Dutch",
            "Portuguese_European",
            "Portuguese_Brazilian",
            "Chinese",
            "Chinese_Trad",
            "Japanese",
            "Korean",
            "Hindi",
            "Thai",
            "Abenaki",
            "Croatian",
            "Georgian",
            "Greek",
            "Serbian",
            "Ukrainian",
            "Latvian"
        };

        public static bool IsLanguageSupported(string language)
        {
            return languages.Contains(language);
        }
    }
}
