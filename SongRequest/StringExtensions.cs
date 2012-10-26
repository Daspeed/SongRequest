using System;
using System.Text.RegularExpressions;

namespace SongRequest
{
    public static class StringExtensions
    {
        public static bool ContainsOrdinalIgnoreCase(this string source, string value, bool replaceSpecialCharacters)
        {
            if (replaceSpecialCharacters)
                return ReplaceCharacters(source).IndexOf(ReplaceCharacters(value), StringComparison.OrdinalIgnoreCase) > -1;

            return source.IndexOf(value, StringComparison.OrdinalIgnoreCase) > -1;
        }

        /// <summary>
        /// Replace characters for searching (like bløf -> blof ect)
        /// </summary>
        private static string ReplaceCharacters(string source)
        {
            string newString = source;

            // replace special characters with an alternative
            newString = newString
                .Replace('á', 'a').Replace('à', 'a').Replace('â', 'a').Replace('ã', 'a').Replace('ä', 'a').Replace('å', 'a').Replace("æ", "ae")
                .Replace('Á', 'A').Replace('À', 'A').Replace('Â', 'A').Replace('Ã', 'A').Replace('Ä', 'A').Replace('Å', 'A').Replace("Æ", "AE")
                .Replace('ç', 'c')
                .Replace('Ç', 'C')
                .Replace('è', 'e').Replace('é', 'e').Replace('ê', 'e').Replace('ë', 'e')
                .Replace('È', 'E').Replace('É', 'E').Replace('Ê', 'E').Replace('Ë', 'E')
                .Replace('ì', 'i').Replace('í', 'i').Replace('î', 'i').Replace('ï', 'i')
                .Replace('Ì', 'I').Replace('Í', 'I').Replace('Î', 'I').Replace('Ï', 'I')
                .Replace('ð', 'd')
                .Replace('Ð', 'D')
                .Replace('ñ', 'n')
                .Replace('Ñ', 'N')
                .Replace('ò', 'o').Replace('ó', 'o').Replace('ô', 'o').Replace('õ', 'o').Replace('ö', 'o').Replace('ø', 'o')
                .Replace('Ò', 'O').Replace('Ó', 'O').Replace('Ô', 'O').Replace('Õ', 'O').Replace('Ö', 'O').Replace('Ø', 'O')
                .Replace('ù', 'u').Replace('ú', 'u').Replace('û', 'u').Replace('ü', 'u')
                .Replace('Ù', 'U').Replace('Ú', 'U').Replace('Û', 'U').Replace('Ü', 'U')
                .Replace('ý', 'y').Replace('ÿ', 'y')
                .Replace('Ý', 'Y').Replace('Ÿ', 'Y');

            // remove characters that are not a-z or 0-9 or _\. 
            newString = Regex.Replace(newString, @"[^a-zA-Z0-9_\. -\\]+", string.Empty, RegexOptions.Compiled);
            return newString;
        }
    }
}
