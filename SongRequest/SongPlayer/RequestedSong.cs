using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SongRequest
{
    public class RequestedSong
    {
        public Song Song { get; set; }
        public string RequesterName { get; set; }
        public DateTime RequestedDate { get; set; }
    }
}
