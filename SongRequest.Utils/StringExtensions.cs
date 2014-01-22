using System;
using System.Globalization;
using System.Threading;

namespace SongRequest.Utils
{
    public static class StringExtensions
    {
        public static bool ContainsOrdinalIgnoreCase(this string source, string value)
        {
            return source.IndexOf(value, StringComparison.OrdinalIgnoreCase) > -1;
        }

        public static bool ContainsIgnoreCaseNonSpace(this string source, string value)
        {
            return Thread.CurrentThread.CurrentCulture.CompareInfo.IndexOf(source, value, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) > -1;
        }

        public static string ReplaceUniqueCharacters(this string source)
        {
            string value = source;

            string[] removeAnyway = new[] { "(", ")", "[", "]", "'", ".", "@" };

            for (int i = 0; i < removeAnyway.Length; i++)
            {
                value = value.Replace(removeAnyway[i], string.Empty);
            }

            string[] bad = new[] { "$", "¹", "²", "³" };
            string[] good = new[] { "s", "1", "2", "3" };

            for (int i = 0; i < bad.Length; i++)
            {
                value = value.Replace(bad[i], good[i]);
            }

            return value;
        }
    }
}
