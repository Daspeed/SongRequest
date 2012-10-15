using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SongRequest
{
    public static class StringExtensions
    {
        public static bool ContainsOrdinalIgnoreCase(this string source, string value)
        {
            return source.IndexOf(value, StringComparison.OrdinalIgnoreCase) > -1;
        }
    }
}
