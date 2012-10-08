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
		private DateTime _currentSongStart;
		private Random random = new Random();

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
		}
		
		public PlayerStatus PlayerStatus 
		{
			get
			{
				if ( _currentSong == null ||
				     _currentSong.Duration == null ||
				    (_currentSong.Duration.Value - (DateTime.Now - _currentSongStart)).TotalSeconds == 0)
				{
					if (_queue.Count > 0)
					{					
						//Take next song from queue
						_currentSong = _queue[0];
						
						_queue.Remove(_currentSong);
					} else
					{
						//Take random song
						_currentSong = _songs[random.Next(_songs.Count - 1)];
					}
					
					_currentSongStart = DateTime.Now;
				}
				
				PlayerStatus playerStatus = new PlayerStatus();
				playerStatus.Song = _currentSong;
				playerStatus.Position = DateTime.Now - _currentSongStart;
					
				return playerStatus;
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

