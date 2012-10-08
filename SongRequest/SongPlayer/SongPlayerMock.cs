using System;
using System.Linq;
using System.Collections.Generic;

namespace SongRequest
{
	public class SongPlayerMock : ISongplayer
	{
		private SongLibrary songLibrary;
		
		private List<Song> _queue;
		private Song _currentSong;
		private DateTime _currentSongStart;

		public SongPlayerMock ()
		{
			songLibrary = new SongLibrary();
			
			songLibrary.AddSong(new Song(){ Artist="4 Strings", Name="Summer Sun", Duration = (int)TimeSpan.FromSeconds(6).TotalSeconds, FileName="4.mp3"});
            songLibrary.AddSong(new Song() { Artist = "Adele", Name = "Set Fire To The Rain", Duration = (int)TimeSpan.FromSeconds(41).TotalSeconds, FileName = "A.mp3" });
            songLibrary.AddSong(new Song() { Artist = "Silverblue", Name = "Step Back", Duration = (int)TimeSpan.FromSeconds(34).TotalSeconds, FileName = "S.mp3" });
            songLibrary.AddSong(new Song() { Artist = "Ilse DeLange", Name = "I'm not so tough", Duration = (int)TimeSpan.FromSeconds(52).TotalSeconds, FileName = "I.mp3" });
            songLibrary.AddSong(new Song() { Artist = "Coldplay", Name = "Clocks", Duration = (int)TimeSpan.FromSeconds(33).TotalSeconds, FileName = "C.mp3" });
            songLibrary.AddSong(new Song() { Artist = "Queen", Name = "Bohemian Rapsody", Duration = (int)TimeSpan.FromSeconds(21).TotalSeconds, FileName = "Q.mp3" });
            songLibrary.AddSong(new Song() { Artist = "The prodigy", Name = "Smack My Bitch Up", Duration = (int)TimeSpan.FromSeconds(36).TotalSeconds, FileName = "Q.mp3" });
			
			_queue = new List<Song>(songLibrary.GetSongs(string.Empty, 0, 3));
		}
		
		public PlayerStatus PlayerStatus 
		{
			get
			{
				if ( _currentSong == null ||
				     _currentSong.Duration == null ||
				    (_currentSong.Duration.Value - (DateTime.Now - _currentSongStart).TotalSeconds) <= 0)
				{
                    Next();
				}
				
				PlayerStatus playerStatus = new PlayerStatus();
				playerStatus.Song = _currentSong;
				playerStatus.Position = (int)(DateTime.Now - _currentSongStart).TotalSeconds;
					
				return playerStatus;
			}
		}
		
		public IEnumerable<Song> GetPlayList(string filter, int skip, int count)
		{
    		return songLibrary.GetSongs(filter, skip, count);
		}
		
		public IEnumerable<Song> PlayQueue 
		{
			get
			{
				return _queue;
			}
		}

        public void Next()
        {
            if (_queue.Count > 0)
            {
                //Take next song from queue
                _currentSong = _queue[0];

                _queue.Remove(_currentSong);
            }
            else
            {
                //Take random song
                _currentSong = songLibrary.GetRandomSong();
            }

            _currentSongStart = DateTime.Now;
        }
		
		public void Enqueue(Song song)
		{
			_queue.Add(song);
		}
		
		public void Dequeue(Song song)
		{
			_queue.Remove(song);
		}
	}
}

