using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SongRequest
{
    [Serializable]
    public class Song
    {
        private static long _count;

        public Song()
        {
            _count++;
            TempId = _count;
            TagRead = false;
        }

        public long TempId { get; private set; }
        public string Artist { get; set; }
        public string Name { get; set; }
        public string FileName { get; set; }
        public int? Duration { get; set; }
        public bool TagRead { get; set; }
    }
}
