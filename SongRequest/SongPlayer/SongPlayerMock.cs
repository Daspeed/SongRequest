using System;
using System.Linq;
using System.Collections.Generic;

namespace SongRequest
{
	public class SongPlayerMock : ISongplayer
	{
		private List<Song> _songs;
		private List<Song> _queue;
		private Song _currentSong;
		
		public SongPlayerMock ()
		{
			_songs = new List<Song> {
							            new Song(){ Artist="4 Strings", Name="Summer Sun", Duration = TimeSpan.FromSeconds(320), FileName="4.mp3"},
							            new Song(){ Artist="Adele", Name="Set Fire To The Rain", Duration = TimeSpan.FromSeconds(410), FileName="A.mp3"},
							            new Song(){ Artist="Silverblue", Name="Step Back", Duration = TimeSpan.FromSeconds(340), FileName="S.mp3"},
							            new Song(){ Artist="Ilse DeLange", Name="I'm not so tough", Duration = TimeSpan.FromSeconds(523), FileName="I.mp3"},
							            new Song(){ Artist="Coldplay", Name="Clocks", Duration = TimeSpan.FromSeconds(333), FileName="C.mp3"},
							            new Song(){ Artist="Queen", Name="Bohemian Rapsody", Duration = TimeSpan.FromSeconds(621), FileName="Q.mp3"},
										new Song(){ Artist="The prodigy", Name="Smack My Bitch Up", Duration = TimeSpan.FromSeconds(536), FileName="Q.mp3"},
							        };
			
			_queue = new List<Song>(_songs.Take(3));	
			
			_currentSong = _songs[0];
		}
		
		public Song CurrentSong 
		{
			get
			{
				return _currentSong;
			}
		}
		
		public IEnumerable<Song> PlayList 
		{
			get
			{
				return _songs;
			}
		}
		
		public IEnumerable<Song> PlayQueue 
		{
			get
			{
				return _queue;
			}
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

