using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SongRequest
{
    public class SongLibrary
    {
        private static object lockObject = new object();
        private Random random = new Random(Environment.TickCount);
        private List<Song> _songs;
        private DateTime _lastFullUpdate;
        private DateTime _lastFixErrors;
        private DateTime _lastSerialize;
        private bool _unsavedChanges;
        public event StatusChangedEventHandler StatusChanged;

        public SongLibrary()
        {
            _songs = new List<Song>();
            _lastFullUpdate = DateTime.Now - TimeSpan.FromDays(1000);
            _lastFixErrors = DateTime.Now - TimeSpan.FromDays(1000);
            _lastSerialize = DateTime.Now;

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
            }
            else
            {
                OnStatusChanged("Library update completed (" + _songs.Count() + " songs). Next scan: " + (_lastFullUpdate + TimeSpan.FromMinutes(minutesBetweenScans)).ToShortTimeString());
            }

            _unsavedChanges = _unsavedChanges || tagChanges > 0;

            //Save, but no more than once every 2 minutes
            if (_unsavedChanges && _lastSerialize + TimeSpan.FromMinutes(2) < DateTime.Now)
                Serialize();

            //No need to scan...
            if (_lastFullUpdate + TimeSpan.FromMinutes(minutesBetweenScans) > DateTime.Now)
                return tagChanges > 0;

            int fileChanges = ScanSongs();

            if (fileChanges > 0 || tagChanges > 0)
            {
                int songCount = _songs.Count();
                int noTagCount = _songs.Count(s => s.TagRead);
                OnStatusChanged(string.Format("Library updated: {0} songs. Tags read: {1}/{0}. Next scan: {2}.", songCount, noTagCount, (_lastFullUpdate + TimeSpan.FromMinutes(minutesBetweenScans)).ToShortTimeString()));
            }

            _lastFullUpdate = DateTime.Now;

            if (fileChanges == 0 || tagChanges == 0)
                OnStatusChanged("Library update completed (" + _songs.Count() + " songs). Next scan: " + (_lastFullUpdate + TimeSpan.FromMinutes(minutesBetweenScans)).ToShortTimeString());

            _unsavedChanges = _unsavedChanges || fileChanges > 0 || tagChanges > 0;

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
                        OnStatusChanged("Saved library to file");
                    }
                    _unsavedChanges = false;
                    _lastSerialize = DateTime.Now;
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

            _unsavedChanges = false;
        }

        private int ScanSongs()
        {
            int changesMade = 0;
            Config.ConfigFile config = SongPlayerFactory.GetConfigFile();
            string[] directories = config.GetValue("library.path").Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            string[] extensions = config.GetValue("library.extensions").Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            if (extensions.Length == 0)
                extensions = new string[] { "mp3" };

            List<string> files = new List<string>();

            //assuming we could have several dirs here, lets speed up the process
            Parallel.ForEach(directories, directory =>
            {
                if (Directory.Exists(directory))
                {
                    foreach (var extension in extensions)
                    {
                        // lock files object
                        lock (lockObject)
                        {
                            files.AddRange(Directory.GetFiles(directory, "*." + extension, SearchOption.AllDirectories).AsEnumerable<string>());
                        }
                    }
                }
            });

            //Find removed songs
            lock (lockObject)
            {
                if (_songs.RemoveAll(s => !files.Any(f => f == s.FileName)) > 0)
                    changesMade++;
            }

            //Find added songs. Here we can have thousands of files
            Parallel.ForEach(files, fileName =>
            {
                bool checkNext = false;

                lock (lockObject)
                {
                    if (_songs.Any(s => s.FileName == fileName))
                        checkNext = true;
                }

                if (!checkNext)
                {
                    FileInfo fileInfo = new FileInfo(fileName);
                    Song song = new Song();
                    song.FileName = fileName;
                    song.Name = Regex.Replace(fileInfo.Name, @"\" + fileInfo.Extension + "$", string.Empty, RegexOptions.IgnoreCase);
                    song.DateCreated = fileInfo.CreationTime.ToString("yyyy-MM-dd HH:mm");

                    lock (lockObject)
                    {
                        _songs.Add(song);
                    }

                    changesMade++;
                }
            });

            return changesMade;
        }

        private int UpdateTags()
        {
            Song song;
            int songsTagged = 0;

            bool fixErrors = DateTime.Now > _lastFixErrors + TimeSpan.FromMinutes(2);

            do
            {
                //Lock collection as short as possible
                lock (lockObject)
                {
                    if (fixErrors)
                        song = _songs.FirstOrDefault(s => s.TagRead == false);
                    else
                        song = _songs.FirstOrDefault(s => s.TagRead == false && s.ErrorReadingTag == false);
                }

                if (song != null)
                {
                    if (song.ErrorReadingTag)
                        _lastFixErrors = DateTime.Now;

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
                        song.Genre = taglibFile.Tag.JoinedGenres;
                        uint year = taglibFile.Tag.Year;
                        song.Year = year > 0 ? year.ToString() : string.Empty;

                        FileInfo fileInfo = new FileInfo(song.FileName);
                        song.DateCreated = fileInfo.CreationTime.ToString("yyyy-MM-dd HH:mm");
                    }
                }
                song.TagRead = true;
                song.ErrorReadingTag = false;
            }
            catch (System.IO.IOException)
            {
                song.TagRead = false;
                song.ErrorReadingTag = true;
            }
            catch (Exception)
            {
                song.TagRead = true;
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
                    Match match = Regex.Match(filter, @"^(f|r|rf|fr):(.+)$");

                    bool includeFileNameInSearch = match.Success && match.Groups[1].Value.Contains("f");

                    Func<string, bool> searchFunc = (source) => StringExtensions.ContainsIgnoreCaseNonSpace(source, includeFileNameInSearch ? match.Groups[2].Value : filter);

                    if (match.Success && match.Groups[1].Value.Contains("r"))
                    {
                        Regex regex = new Func<Regex>(() =>
                        {
                            try { return new Regex(match.Groups[2].Value, RegexOptions.IgnoreCase); }
                            catch (Exception) { return null; }
                        })();

                        if (regex != null)
                            searchFunc = regex.IsMatch;
                    }

                    songs = _songs.AsParallel().Where(s =>
                        searchFunc(s.Name ?? string.Empty) ||
                        searchFunc(s.Artist ?? string.Empty) ||
                        (includeFileNameInSearch ? searchFunc(s.FileName ?? string.Empty) : false)
                    );
                }

                SortBy importantSort;
                switch (sortBy)
                {
                    case "date":
                        importantSort = SortBy.Date;
                        break;
                    case "name":
                        importantSort = SortBy.Name;
                        break;
                    case "artist":
                    default:
                        importantSort = SortBy.Artist;
                        break;
                }

                Func<IEnumerable<Song>, Func<Song, string>, IComparer<string>, IOrderedEnumerable<Song>> firstSorter = Enumerable.OrderBy;
                if (!ascending)
                    firstSorter = Enumerable.OrderByDescending;

                Func<IOrderedEnumerable<Song>, Func<Song, string>, IComparer<string>, IOrderedEnumerable<Song>> secondSorter = Enumerable.ThenBy;
                if (!ascending)
                    secondSorter = Enumerable.ThenByDescending;

                Func<IOrderedEnumerable<Song>, Func<Song, string>, IComparer<string>, IOrderedEnumerable<Song>> thirdSorter = Enumerable.ThenByDescending;
                if (!ascending)
                    thirdSorter = Enumerable.ThenBy;

                return
                    thirdSorter(
                        secondSorter(
                            firstSorter(songs,
                                x => { return ((importantSort == SortBy.Artist) ? x.Artist : (importantSort == SortBy.Name ? x.Name : x.DateCreated)); }, StringComparer.OrdinalIgnoreCase),
                                x => { return ((importantSort == SortBy.Artist) ? x.Name : (importantSort == SortBy.Name ? x.Artist : x.Artist)); }, StringComparer.OrdinalIgnoreCase),
                                x => { return ((importantSort == SortBy.Artist) ? x.TempId : (importantSort == SortBy.Name ? x.TempId : x.Name)); }, StringComparer.OrdinalIgnoreCase
                                );
            }
        }

        private enum SortBy
        {
            Artist,
            Name,
            Date
        }

        public void Rescan()
        {
            lock (_songs)
            {
                Parallel.ForEach(_songs, song =>
                {
                    _lastFullUpdate = DateTime.Now - TimeSpan.FromDays(1000);
                    song.TagRead = false;
                });
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

