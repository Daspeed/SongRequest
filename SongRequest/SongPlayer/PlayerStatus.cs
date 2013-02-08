using System;

namespace SongRequest
{
    public class PlayerStatus
    {
        public RequestedSong RequestedSong
        {
            get;
            set;
        }

        public int? Position
        {
            get;
            set;
        }

        public int Volume { get; set; }
    }
}
