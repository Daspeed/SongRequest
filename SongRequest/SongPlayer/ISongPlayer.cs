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

        IEnumerable<RequestedSong> PlayQueue { get; }

        void Next();

        void Enqueue(long id, string requesterName);
        void Enqueue(Song song, string requesterName);

        void Dequeue(long id);
		void Dequeue(Song song);

        int Volume { get; set; }
	}
}

