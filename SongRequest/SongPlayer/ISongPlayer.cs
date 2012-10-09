using System;
using System.Collections.Generic;

namespace SongRequest
{
    public delegate void StatusChangedEventHandler(string status);

	public interface ISongplayer
	{
        event StatusChangedEventHandler LibraryStatusChanged;
        event StatusChangedEventHandler PlayerStatusChanged;

		PlayerStatus PlayerStatus {get;}
		
        IEnumerable<Song> GetPlayList(string filter, int skip, int count);
		
		IEnumerable<Song> PlayQueue {get;}

        void Next();

		void Enqueue(Song song);
		
		void Dequeue(Song song);

        int Volume { get; set; }
	}
}

