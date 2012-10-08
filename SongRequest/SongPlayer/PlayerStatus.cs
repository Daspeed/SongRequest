using System;

namespace SongRequest
{
	public class PlayerStatus
	{
		public Song Song {
			get;
			set;
		}
		
		public int? Position {
			get;
			set;
		}

        public int Volume { get; set; }
	}
}

