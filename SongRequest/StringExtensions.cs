using System;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Threading;

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
            return Thread.CurrentThread.CurrentCulture.CompareInfo
                .IndexOf(source, value, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) > -1;
        }
    }
}
