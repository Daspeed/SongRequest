using System;

namespace SongRequest
{
    public class RequestedSong
    {
        public Song Song { get; set; }
        public string RequesterName { get; set; }
        public DateTime RequestedDate { get; set; }
    }
}
