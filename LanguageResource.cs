using System;
using System.Collections.Generic;
using System.Text;

namespace VectorCharMap
{
    public static class LanguageResource
    {
        public static Dictionary<string, string> LanguageDictionary { get; private set; } = new Dictionary<string, string>();

        public static void LoadLanguage(string language)
        {
            LanguageDictionary = new Dictionary<string, string>();

            var langFile = new System.IO.FileInfo($"{language}.lg");

            var stringData = new StringBuilder();

            using (var fs = langFile.OpenRead())
            {
                var data = new byte[fs.Length];
                var readBytesCount = fs.Read(data, 0, data.Length);

                if (readBytesCount == 0)
                {
                    fs.Close();
                    fs.Flush();
                    return;
                }

                stringData.Append(Encoding.UTF8.GetString(data));
            }

            var entries = stringData.ToString().Split('\n');

            foreach (var entry in entries)
            {
                var index = entry.IndexOf('=');

                var key = "";

                for (int i = 0; i < index; i++)
                {
                    key += entry[i];
                }

                var value = "";

                for (int i = index + 1; i < entry.Length; i++)
                {
                    value += entry[i];
                }

                LanguageDictionary.TryAdd(key, value);
            }
        }
    }
}
