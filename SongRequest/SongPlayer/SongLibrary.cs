using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;
using System.Threading;

namespace SongRequest
{
	public class SongLibrary
	{
		private static object lockObject = new object();
		private Random random = new Random(Environment.TickCount);
		private List<Song> _songs;
		private DateTime _lastFullUpdate;
		public event StatusChangedEventHandler StatusChanged;

		public SongLibrary()
		{
			_songs = new List<Song>();
            _lastFullUpdate = DateTime.Now - TimeSpan.FromDays(1000);

			OnStatusChanged("Library created...");
			Deserialize();
		}

		protected virtual void OnStatusChanged(string status)
		{
			if (StatusChanged != null)
				StatusChanged(status);
		}

		public bool ScanLibrary()
		{
            int minutesBetweenScans;

            if (!int.TryParse(SongPlayerFactory.GetConfigFile().GetValue("library.minutesbetweenscans"), out minutesBetweenScans))
                minutesBetweenScans = 2;

			int tagChanges = UpdateTags();
            if (tagChanges > 0)
            {
                int songCount = _songs.Count();
                int noTagCount = _songs.Count(s => s.TagRead);
                OnStatusChanged(string.Format("Library updated: {0} songs. Tags read: {1}/{0}. Next scan: {2}.", songCount, noTagCount, (_lastFullUpdate + TimeSpan.FromMinutes(minutesBetweenScans)).ToShortTimeString()));
                Serialize();
                OnStatusChanged(string.Format("Library updated: {0} songs. Tags read: {1}/{0}. Next scan: {2}. (saved)", songCount, noTagCount, (_lastFullUpdate + TimeSpan.FromMinutes(minutesBetweenScans)).ToShortTimeString()));
            }
            else
            {
                OnStatusChanged("Library update completed (" + _songs.Count() + " songs). Next scan: " + (_lastFullUpdate + TimeSpan.FromMinutes(minutesBetweenScans)).ToShortTimeString());
            }

			//No need to scan...
            if (_lastFullUpdate + TimeSpan.FromMinutes(minutesBetweenScans) > DateTime.Now)
                return tagChanges > 0;

			int fileChanges = ScanSongs();
			
			if (fileChanges > 0 || tagChanges > 0)
			{
				int songCount = _songs.Count();
				int noTagCount = _songs.Count(s => s.TagRead);
                OnStatusChanged(string.Format("Library updated: {0} songs. Tags read: {1}/{0}. Next scan: {2}.", songCount, noTagCount, (_lastFullUpdate + TimeSpan.FromMinutes(minutesBetweenScans)).ToShortTimeString()));
				Serialize();
                OnStatusChanged(string.Format("Library updated: {0} songs. Tags read: {1}/{0}. Next scan: {2}. (saved)", songCount, noTagCount, (_lastFullUpdate + TimeSpan.FromMinutes(minutesBetweenScans)).ToShortTimeString()));
			}



            _lastFullUpdate = DateTime.Now;

            if (fileChanges == 0 || tagChanges == 0)
                OnStatusChanged("Library update completed (" + _songs.Count() + " songs). Next scan: " + (_lastFullUpdate + TimeSpan.FromMinutes(minutesBetweenScans)).ToShortTimeString());

            return fileChanges > 0 || tagChanges > 0;
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
			string[] directories = SongPlayerFactory.GetConfigFile().GetValue("library.path").Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            string[] extensions = SongPlayerFactory.GetConfigFile().GetValue("library.extensions").Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            if (extensions.Length == 0)
                extensions = new string[] { "mp3" };

			List<string> files = new List<string>();
			foreach (string directory in directories)
			{
				if (Directory.Exists(directory))
				{
                    foreach (string extension in extensions)
                    {
                        files.AddRange(Directory.GetFiles(directory, "*." + extension, SearchOption.AllDirectories).AsEnumerable<string>());
                    }
				}
			}

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
                FileInfo fileInfo= new FileInfo(fileName);
                song.Name = Regex.Replace(fileInfo.Name, @"\" + fileInfo.Extension + "$", string.Empty, RegexOptions.IgnoreCase);

				AddSong(song);

				changesMade++;
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
                    UpdateSingleTag(song);
					songsTagged++;
				}
			} while (song != null && songsTagged < 200);

			return songsTagged;
		}

        public static void UpdateSingleTag(Song song)
        {
            try
            {

                using (TagLib.File taglibFile = TagLib.File.Create(song.FileName))
                {
                    if (taglibFile.Tag != null)
                    {
                        if (!string.IsNullOrEmpty(taglibFile.Tag.Title))
                            song.Name = taglibFile.Tag.Title;

                        song.Artist = taglibFile.Tag.JoinedPerformers;
                        song.Duration = (int)taglibFile.Properties.Duration.TotalSeconds;
                    }
                }
                song.TagRead = true;
            }
            catch(System.IO.IOException)
            {
                song.TagRead = false;
                Thread.Sleep(100);
            }
            catch (Exception)
            {
                song.TagRead = true;
            }                        
        }

		public void AddSong(Song song)
		{
			lock (lockObject)
			{
				_songs.Add(song);
			}
		}

        public IEnumerable<Song> GetSongs(string filter, string sortBy, bool ascending)
		{
			lock (lockObject)
			{
                IEnumerable<Song> songs = null;
                if (string.IsNullOrEmpty(filter))
                {
                    songs = _songs;
                }
                else
                {
                    songs = _songs.Where(s => (s.FileName ?? string.Empty).ContainsOrdinalIgnoreCase(filter) ||
                                        (s.Name ?? string.Empty).ContainsOrdinalIgnoreCase(filter) ||
                                        (s.Artist ?? string.Empty).ContainsOrdinalIgnoreCase(filter)
                                   );
                }

                bool sortbyArtist = sortBy == "artist";

                Func<IEnumerable<Song>, Func<Song, string>, IComparer<string>, IOrderedEnumerable<Song>> firstSorter = Enumerable.OrderBy;
                if(!ascending)
                    firstSorter = Enumerable.OrderByDescending;

                Func<IOrderedEnumerable<Song>, Func<Song, string>, IComparer<string>, IOrderedEnumerable<Song>> secondSorter = Enumerable.ThenBy;
                if (!ascending)
                    secondSorter = Enumerable.ThenByDescending;

                return
                    secondSorter(
                        firstSorter(songs, x => { return sortbyArtist ? x.Artist : x.Name; }, StringComparer.OrdinalIgnoreCase),
                        x => { return sortbyArtist ? x.Name : x.Artist; },
                        StringComparer.OrdinalIgnoreCase
                    );
			}
		}

        public void Rescan()
        {
            lock (_songs)
            {
                foreach (var song in _songs)
                {
                    _lastFullUpdate = DateTime.Now - TimeSpan.FromDays(1000);
                    song.TagRead = false;
                }
            }
        }

		public RequestedSong GetRandomSong()
		{
			lock (lockObject)
			{
				if (_songs.Count == 0)
					return null;

                Song randomSong = _songs[random.Next(_songs.Count)];

                //If song is > 10 minutes, pick another song... But don't try ferever...
                if (randomSong.Duration != null &&
                    randomSong.Duration > 600)
                    randomSong = _songs[random.Next(_songs.Count)];

				return new RequestedSong()
				{
                    Song = randomSong,
					RequesterName = "randomizer"
				};
			}
		}
	}
}

