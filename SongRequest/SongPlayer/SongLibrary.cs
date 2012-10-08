using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;

namespace SongRequest
{
	public class SongLibrary
	{
		private Random random = new Random();
		private List<Song> _songs;
		
		public SongLibrary ()
		{
			_songs = new List<Song>();
		}
		
		public void ScanSongs(string directory)
		{
			foreach(string fileName in Directory.GetFiles(directory, "*.mp3", SearchOption.AllDirectories))
			{
				//Do some magic...
			}
		}		
		
		public void AddSong(Song song)
		{
			_songs.Add(song);
		}
		
		public IEnumerable<Song> GetSongs(string filter, int top, int count)
		{
			return _songs.Where(s => s.FileName.ToLower().Contains(filter.ToLower())).Skip(top).Take(count);
		}
		
		public Song GetRandomSong()
		{
			return _songs[random.Next(_songs.Count() - 1)];
		}
	}
}

