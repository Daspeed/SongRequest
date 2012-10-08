using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using Id3;

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
                Song song = new Song();
                song.FileName = fileName;

                using (var mp3 = new Mp3File(fileName))
                {
                    Id3Tag tag = mp3.GetTag(Id3TagFamily.FileStartTag);
                    song.Name = tag.Title.Value;
                    song.Artist = tag.Artists.Value;
                    song.Duration = mp3.Audio.Duration;
                }

                _songs.Add(song);

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

