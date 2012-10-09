using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace SongRequest
{
    public class SongLibrary
    {
        private object lockObject = new object();
        private Random random = new Random(Environment.TickCount);
        private List<Song> _songs;
        private string _directory;
        private DateTime _nextFullUpdate;
        public event StatusChangedEventHandler StatusChanged;

        public SongLibrary(string directory)
        {
            _songs = new List<Song>();
            _directory = directory;
            _nextFullUpdate = DateTime.Now;

            OnStatusChanged("Library created...");
            Deserialize();
        }

        protected virtual void OnStatusChanged(string status)
        {
            if (StatusChanged != null)
                StatusChanged(status);
        }

        public void ScanLibrary()
        {
            //No need to scan...
            if (_nextFullUpdate > DateTime.Now)
                return;

            int fileChanges = ScanSongs();
            int tagChanges = UpdateTags();
            if (fileChanges > 0 || tagChanges > 0)
            {
                int songCount = _songs.Count();
                int noTagCount = _songs.Count(s => !s.TagRead);
                OnStatusChanged(string.Format("Library updated: {0} songs. Tags read: {1}/{0}", songCount, noTagCount));
                Serialize();
                OnStatusChanged(string.Format("Library updated: {0} songs. Tags read: {1}/{0} (saved)", songCount, noTagCount));
            }
            else
            {
                int minutesBetweenScans;

                if (!int.TryParse(SongPlayerFactory.GetConfigFile().GetValue("library.minutesbetweenscans"), out minutesBetweenScans))
                    minutesBetweenScans = 2;

                _nextFullUpdate = DateTime.Now + TimeSpan.FromMinutes(minutesBetweenScans);
                OnStatusChanged("Library update completed (" + _songs.Count() + " songs). Next scan: " + _nextFullUpdate.ToShortTimeString());
            }
        }

        private void Serialize()
        {
            try
            {
                lock (lockObject)
                {
                    using (Stream stream = File.Open("library.bin", FileMode.Create))
                    {
                        BinaryFormatter bin = new BinaryFormatter();
                        bin.Serialize(stream, _songs);
                    }
                }
            }
            catch (IOException)
            {
            }
        }

        private void Deserialize()
        {
            try
            {
                lock (lockObject)
                {
                    if (File.Exists("library.bin"))
                    {
                        using (Stream stream = File.Open("library.bin", FileMode.Open))
                        {
                            if (stream.Length > 0)
                            {
                                BinaryFormatter bin = new BinaryFormatter();
                                _songs = (List<Song>)bin.Deserialize(stream);
                            }
                        }
                    }
                }

                OnStatusChanged("Loaded library containing " + _songs.Count() + " songs");
            }
            catch (IOException)
            {
                OnStatusChanged("Error loading library...");
            }
        }

        private int ScanSongs()
        {
            int changesMade = 0;
            if (Directory.Exists(_directory))
            {
                string[] files = Directory.GetFiles(_directory, "*.mp3", SearchOption.AllDirectories);

                //Find removed songs
                lock (lockObject)
                {
                    if (_songs.RemoveAll(s => !files.Any(f => f == s.FileName)) > 0)
                        changesMade++;
                }

                //Find added songs
                foreach (string fileName in files)
                {
                    lock (lockObject)
                    {
                        if (_songs.Any(s => s.FileName == fileName))
                            continue;
                    }

                    Song song = new Song();
                    song.FileName = fileName;
                    song.Name = fileName;

                    AddSong(song);

                    changesMade++;
                }
            }

            return changesMade;
        }

        private int UpdateTags()
        {
            Song song;
            int songsTagged = 0;

            do
            {
                //Lock collection as short as possible
                lock (lockObject)
                {
                    song = _songs.FirstOrDefault(s => s.TagRead == false);
                }

                if (song != null)
                {
                    try
                    {

                        TagLib.File taglibFile = TagLib.File.Create(song.FileName);

                        if (taglibFile.Tag != null)
                        {
                            if (!string.IsNullOrEmpty(taglibFile.Tag.Title))
                                song.Name = taglibFile.Tag.Title;

                            song.Artist = taglibFile.Tag.JoinedPerformers;
                            song.Duration = (int)taglibFile.Properties.Duration.TotalSeconds;
                        }
                    }
                    catch
                    {
                    }

                    song.TagRead = true;
                    songsTagged++;
                }
            } while (song != null && songsTagged < 200);

            return songsTagged;
        }

        public void AddSong(Song song)
        {
            lock (lockObject)
            {
                _songs.Add(song);
            }
        }

        public IEnumerable<Song> GetSongs(string filter, int skip, int count)
        {
            lock (lockObject)
            {
                return _songs.Where(s => (s.FileName??string.Empty).ToLower().Contains(filter.ToLower()) ||
                                         (s.Name??string.Empty).ToLower().Contains(filter.ToLower()) ||
                                         (s.Artist??string.Empty).ToLower().Contains(filter.ToLower())                    
                                    ).Skip(skip).Take(count);
            }
        }

        public Song GetRandomSong()
        {
            lock (lockObject)
            {
                if (_songs.Count == 0)
                    return null;

                return _songs[random.Next(_songs.Count)];
            }
        }
    }
}

