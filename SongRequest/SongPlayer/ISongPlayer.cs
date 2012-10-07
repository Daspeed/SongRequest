using System;
using System.Collections.Generic;

namespace SongRequest
{
	public interface ISongplayer
	{
		Song CurrentSong {get;}
		
		IEnumerable<Song> PlayList {get;}
		
		IEnumerable<Song> PlayQueue {get;}
		
		void Enqueue(Song song);
		
		void Dequeue(Song song);
	}
}

