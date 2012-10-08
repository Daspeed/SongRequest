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

        public SongLibrary()
        {
            _songs = new List<Song>();
        }

        public void ScanSongs(string directory)
        {
            if (Directory.Exists("c:\\music"))
                foreach (string fileName in Directory.GetFiles(directory, "*.mp3", SearchOption.AllDirectories))
                {
                    try
                    {
                        //Do some magic...
                        Song song = new Song();
                        song.FileName = fileName;

                        using (var mp3 = new Mp3File(fileName))
                        {
                            Id3Tag tag = mp3.GetTag(Id3TagFamily.FileStartTag);
                            song.Name = tag == null || tag.Title == null ? "unknown" : tag.Title.Value;
                            song.Artist = tag == null || tag.Artists == null ? "unknown" : tag.Artists.Value;
                            song.Duration = (int)mp3.Audio.Duration.TotalSeconds;
                        }

                        _songs.Add(song);
                    }
                    catch
                    {
                    }

                }
        }

        public void AddSong(Song song)
        {
            _songs.Add(song);
        }

        public IEnumerable<Song> GetSongs(string filter, int skip, int count)
        {
            return _songs.Where(s => s.FileName.ToLower().Contains(filter.ToLower())).Skip(skip).Take(count);
        }

        public Song GetRandomSong()
        {
            if (_songs.Count == 0)
                return null;

            return _songs[random.Next(_songs.Count)];
        }
    }
}

