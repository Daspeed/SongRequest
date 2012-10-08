using System;
using System.Collections.Generic;

namespace SongRequest
{
	public interface ISongplayer
	{
		PlayerStatus PlayerStatus {get;}
		
        IEnumerable<Song> GetPlayList(string filter, int skip, int count);
		
		IEnumerable<Song> PlayQueue {get;}

        void Next();

		void Enqueue(Song song);
		
		void Dequeue(Song song);
	}
}

