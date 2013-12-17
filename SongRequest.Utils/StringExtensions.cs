using System;
using System.Globalization;
using System.Threading;
using DoubleMetaphone;

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
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(value))
                return false;

            string sourceReplaced = source.ToLower().ReplaceUniqueCharacters();
            string valueReplaced = value.ToLower().ReplaceUniqueCharacters();

            bool normal = Thread.CurrentThread.CurrentCulture.CompareInfo.IndexOf(sourceReplaced, valueReplaced, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) > -1;
            if (normal)
                return true;

            string sourceDoubleMetaphone = sourceReplaced.GenerateDoubleMetaphone();
            string valueDoubleMetaphone = valueReplaced.GenerateDoubleMetaphone();

            bool doubleMetaphone = sourceDoubleMetaphone.Equals(valueDoubleMetaphone, StringComparison.Ordinal);
            if (doubleMetaphone)
                return true;

            return false;
        }

        public static string ReplaceUniqueCharacters(this string source)
        {
            string value = source;

            string[] removeAnyway = new[] { "(", ")", "[", "]", "original mix", "remix", "'", ".", "@" };

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
