using System;
using System.Globalization;
using System.Threading;
using DoubleMetaphone;

namespace SongRequest
{
    public static class StringExtensions
    {
        public static bool ContainsOrdinalIgnoreCase(this string source, string value)
        {
            return source.IndexOf(value, StringComparison.OrdinalIgnoreCase) > -1;
        }


        public static bool ContainsIgnoreCaseNonSpace(this string source, string value)
        {
            string sourceReplaced = source.ReplaceUniqueCharacters();
            string valueReplaced = value.ReplaceUniqueCharacters();

            bool normal = Thread.CurrentThread.CurrentCulture.CompareInfo.IndexOf(sourceReplaced, valueReplaced, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) > -1;
            if (normal)
                return true;

            string sourceDoubleMetaphone = source.GenerateDoubleMetaphone();
            string valueDoubleMetaphone = value.GenerateDoubleMetaphone();

            bool doubleMetaphone = sourceDoubleMetaphone.Equals(valueDoubleMetaphone, StringComparison.Ordinal);
            if (doubleMetaphone)
                return true;

            return false;
        }

        public static string ReplaceUniqueCharacters(this string source)
        {
            char[] bad = new char[] { '$', '¹', '²', '³' };
            char[] good = new char[] { 's', '1', '2', '3' };

            string value = source;

            for (int i = 0; i < bad.Length; i++)
            {
                value = value.Replace(bad[i], good[i]);
            }

            return value;
        }
    }
}
