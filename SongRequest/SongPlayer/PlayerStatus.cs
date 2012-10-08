using System;

namespace SongRequest
{
	public class PlayerStatus
	{
		public Song Song {
			get;
			set;
		}
		
		public TimeSpan? Position {
			get;
			set;
		}
	}
}

