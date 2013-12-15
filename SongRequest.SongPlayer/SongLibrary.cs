using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using SongRequest.Utils;

namespace SongRequest
{
    public class SongLibrary
    {
        private static object lockObject = new object();
        private Random random = new Random(Environment.TickCount);
        private Dictionary<string, Song> _songs;
        private DateTime _lastFullUpdate;
        private DateTime _lastFixErrors;
        private DateTime _lastSerialize;
        private bool _unsavedChanges;
        public event StatusChangedEventHandler StatusChanged;

        public SongLibrary()
        {
            _songs = new Dictionary<string, Song>(StringComparer.OrdinalIgnoreCase);
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
                int noTagCount = _songs.Values.Count(s => s.TagRead);
                OnStatusChanged(string.Format("Library updated: {0} songs. Tags read: {1}/{0}. Next scan: {2}.", songCount, noTagCount, (_lastFullUpdate + TimeSpan.FromMinutes(minutesBetweenScans)).ToShortTimeString()));
            }
            else
            {
                OnStatusChanged("Library update completed (" + _songs.Count() + " songs). Next scan: " + (_lastFullUpdate + TimeSpan.FromMinutes(minutesBetweenScans)).ToShortTimeString());
            }

            // check need to save
            // -> unsaved changes
            // -> tag(s) are changed
            // -> song is marked dirty (last time played is changed)
            bool dirtySongs = _songs.Values.Any(x => x.IsDirty);
            _unsavedChanges = _unsavedChanges || tagChanges > 0 || dirtySongs;

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
                int noTagCount = _songs.Values.Count(s => s.TagRead);
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
                    // don't be dirty anymore!
                    List<Song> dirtySongs = _songs.Values.Where(x => x.IsDirty).ToList();
                    foreach (Song song in dirtySongs)
                        song.IsDirty = false;

                    using (Stream stream = File.Open("library.bin", FileMode.Create))
                    {
                        BinaryFormatter bin = new BinaryFormatter();
                        bin.Serialize(stream, _songs.Values);
                        OnStatusChanged("Saved library to file");
                    }

                    _unsavedChanges = false;
                    _lastSerialize = DateTime.Now;
                }
            }
            catch (IOException exception)
            {
                OnStatusChanged("Library saving failed. -> " + exception.Message);
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

                                List<Song> songs = (List<Song>)bin.Deserialize(stream);

                                foreach (Song song in songs)
                                    _songs.Add(song.FileName, song);

                                // can't be dirty when just deserialized...
                                List<Song> dirtySongs = _songs.Values.Where(x => x.IsDirty).ToList();
                                foreach (Song song in dirtySongs)
                                    song.IsDirty = false;
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

        /// <summary>
        /// Get all matching files in a folder
        /// </summary>
        private HashSet<string> GetFilesRecursive(string directory, IList<string> extensions)
        {
            // big list with (at the end) all extension matching files of this folder and it's subfolders
            HashSet<string> files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // change this when error occurrend
            bool doSubdirectories = true;

            // use directory info
            DirectoryInfo currentDirectory = new DirectoryInfo(directory);

            // do every extension parallel
            Parallel.ForEach(extensions, extension =>
            {
                try
                {
                    // store temporary, union later
                    HashSet<string> currentDirectoryFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    // loop trough files
                    FileInfo[] directoryFiles = currentDirectory.GetFiles("*." + extension, SearchOption.TopDirectoryOnly);

                    foreach (FileInfo fileInfo in directoryFiles)
                    {
                        // skip hidden files
                        if (SkipFileOrFolder(fileInfo.FullName))
                            continue;

                        currentDirectoryFiles.Add(fileInfo.FullName);
                    }

                    if (currentDirectoryFiles.Count > 0)
                    {
                        // lock big list, can go wrong
                        lock (lockObject)
                        {
                            files.UnionWith(currentDirectoryFiles);
                        }
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    doSubdirectories = false;
                }
            });

            // do subdirectories if no unauthorized exception occurred
            if (doSubdirectories)
            {
                // get all directories
                foreach (DirectoryInfo subDirectory in currentDirectory.GetDirectories("*", SearchOption.TopDirectoryOnly))
                {
                    // check need to skip (like hidden folder)
                    if (SkipFileOrFolder(subDirectory.FullName))
                        continue;

                    // get files from folder
                    HashSet<string> subDirectoryFiles = GetFilesRecursive(subDirectory.FullName, extensions);
                    if (subDirectoryFiles.Count > 0)
                    {
                        lock (lockObject)
                        {
                            files.UnionWith(subDirectoryFiles);
                        }
                    }
                }
            }

            return files;
        }

        private bool SkipFileOrFolder(string path)
        {
            FileAttributes fileAttribute = File.GetAttributes(path);

            // skip hidden folders
            if ((fileAttribute & FileAttributes.Hidden) > 0)
                return true;

            if ((fileAttribute & FileAttributes.Offline) > 0)
                return true;

            if ((fileAttribute & FileAttributes.System) > 0)
                return true;

            if ((fileAttribute & FileAttributes.Temporary) > 0)
                return true;

            if ((fileAttribute & FileAttributes.ReparsePoint) > 0)
                return true;

            if ((fileAttribute & FileAttributes.SparseFile) > 0)
                return true;

            return false;
        }

        private int ScanSongs()
        {
            int changesMade = 0;
            Config.ConfigFile config = SongPlayerFactory.GetConfigFile();
            string[] directories = config.GetValue("library.path").Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            string[] extensions = config.GetValue("library.extensions").Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            if (extensions.Length == 0)
                extensions = new string[] { "mp3" };

            HashSet<string> files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            //assuming we could have several dirs here, lets speed up the process
            Parallel.ForEach(directories, directory =>
            {
                if (Directory.Exists(directory))
                {
                    HashSet<string> filesFound = GetFilesRecursive(directory, extensions);

                    // lock files object
                    lock (lockObject)
                    {
                        files.UnionWith(filesFound);
                    }
                }
            });

            //Find removed songs
            lock (lockObject)
            {
                var toRemove = _songs.Keys.Where(x => !files.Contains(x, StringComparer.OrdinalIgnoreCase));

                foreach (string key in toRemove)
                {
                    if (_songs.Remove(key))
                        changesMade++;
                }
            }

            //Find added songs. Here we can have thousands of files
            Parallel.ForEach(files, fileName =>
            {
                bool checkNext = false;

                lock (lockObject)
                {
                    if (_songs.ContainsKey(fileName))
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
                        _songs.Add(fileName, song);
                    }

                    changesMade++;
                }
            });

            return changesMade;
        }

        private int UpdateTags()
        {
            if (_songs.Count == 0)
                return 0;

            Song song;
            int songsTagged = 0;

            bool fixErrors = DateTime.Now > _lastFixErrors + TimeSpan.FromMinutes(2);
            List<ManualResetEvent> doneEvents = new List<ManualResetEvent>();

            do
            {
                //Lock collection as short as possible
                lock (lockObject)
                {
                    if (fixErrors)
                        song = _songs.Values.FirstOrDefault(s => s.TagRead == false);
                    else
                        song = _songs.Values.FirstOrDefault(s => s.TagRead == false && s.ErrorReadingTag == false);
                }

                if (song != null)
                {
                    if (song.ErrorReadingTag)
                        _lastFixErrors = DateTime.Now;

                    ManualResetEvent manualResetEvent = new ManualResetEvent(false);
                    doneEvents.Add(manualResetEvent);

                    SongLibrarySongUpdate songUpdate = new SongLibrarySongUpdate(manualResetEvent);
                    ThreadPool.QueueUserWorkItem(songUpdate.ThreadPoolCallback, song);

                    songsTagged++;
                }

                // loop until no song found or more than 64 (max for threadpool!) will be tagged
            } while (song != null && songsTagged < 64);

            if (songsTagged > 0)
            {
                // Wait for all threads in pool to calculate.
                // But only when song's need to be tagged
                WaitHandle.WaitAll(doneEvents.ToArray());
            }

            return songsTagged;
        }

        /// <summary>
        /// Update tags for single song
        /// </summary>
        public static void UpdateSingleTag(Song song)
        {
            try
            {
                using (TagLib.File taglibFile = TagLib.File.Create(song.FileName))
                {
                    if (taglibFile.Tag != null)
                    {
                        if (!string.IsNullOrEmpty(taglibFile.Tag.Title))
                            song.Name = taglibFile.Tag.Title.Trim();

                        if (!string.IsNullOrEmpty(taglibFile.Tag.JoinedPerformers))
                            song.Artist = taglibFile.Tag.JoinedPerformers.Trim();

                        if (!string.IsNullOrEmpty(taglibFile.Tag.JoinedGenres))
                            song.Genre = taglibFile.Tag.JoinedGenres.Trim();

                        song.Duration = (int)taglibFile.Properties.Duration.TotalSeconds;

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
                if (string.IsNullOrWhiteSpace(filter))
                {
                    songs = _songs.Values;
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
                        searchFunc(s.Value.Name ?? string.Empty) ||
                        searchFunc(s.Value.Artist ?? string.Empty) ||
                        (includeFileNameInSearch ? searchFunc(s.Key ?? string.Empty) : false)
                    ).Select(x => x.Value);
                }

                // get correct stuff to sort on
                SortBy importantSort;
                if (string.IsNullOrEmpty(sortBy) || sortBy.Equals("artist", StringComparison.OrdinalIgnoreCase))
                    importantSort = SortBy.Artist;
                else if (sortBy.Equals("date", StringComparison.OrdinalIgnoreCase))
                    importantSort = SortBy.Date;
                else if (sortBy.Equals("playdate", StringComparison.OrdinalIgnoreCase))
                    importantSort = SortBy.PlayDate;
                else if (sortBy.Equals("genre", StringComparison.OrdinalIgnoreCase))
                    importantSort = SortBy.Genre;
                else if (sortBy.Equals("year", StringComparison.OrdinalIgnoreCase))
                    importantSort = SortBy.Year;
                else
                    importantSort = SortBy.Name;

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
                                x => GetSortString(x, importantSort, 0), StringComparer.OrdinalIgnoreCase),
                                x => GetSortString(x, importantSort, 1), StringComparer.OrdinalIgnoreCase),
                                x => GetSortString(x, importantSort, 2), StringComparer.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Get correct sort string
        /// </summary>
        private string GetSortString(Song song, SortBy sortBy, int level)
        {
            if (sortBy == SortBy.Artist)
            {
                switch (level)
                {
                    case 0:
                        return song.Artist;
                    case 1:
                        return song.Name;
                    case 2:
                        return song.Year;
                }
            }
            else if (sortBy == SortBy.Name)
            {
                switch (level)
                {
                    case 0:
                        return song.Name;
                    case 1:
                        return song.Artist;
                    case 2:
                        return song.TempId;
                }
            }
            else if (sortBy == SortBy.Date)
            {
                switch (level)
                {
                    case 0:
                        return song.DateCreated;
                    case 1:
                        return song.Artist;
                    case 2:
                        return song.Name;
                }
            }
            else if (sortBy == SortBy.PlayDate)
            {
                switch (level)
                {
                    case 0:
                        if (song.LastPlayDateTime.HasValue)
                            return song.LastPlayDateTime.Value.ToString("yyyy-MM-dd HH:mm:ss.fff");
                        return string.Empty;
                    case 1:
                        return song.Artist;
                    case 2:
                        return song.Name;
                }
            }
            else if (sortBy == SortBy.Genre)
            {
                switch (level)
                {
                    case 0:
                        return song.Genre;
                    case 1:
                        return song.Artist;
                    case 2:
                        return song.Name;
                }
            }
            else if (sortBy == SortBy.Year)
            {
                switch (level)
                {
                    case 0:
                        return song.Year;
                    case 1:
                        return song.Artist;
                    case 2:
                        return song.Name;
                }
            }

            return string.Empty;
        }

        private enum SortBy
        {
            Artist,
            Name,
            Date,
            PlayDate,
            Genre,
            Year
        }

        public void Rescan()
        {
            lock (lockObject)
            {
                Parallel.ForEach(_songs.Values, song =>
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

                int randomizerIgnoreHours;
                if (!int.TryParse(SongPlayerFactory.GetConfigFile().GetValue("player.randomizerignorehours"), out randomizerIgnoreHours))
                    randomizerIgnoreHours = 8;

                // If song is > 10 minutes, ignore
                // If song is played last xxx hours, ignore
                List<Song> songsToChooseFrom = _songs.Values.Where(x => (x.Duration != null && x.Duration < 600)
                    && (x.LastPlayDateTime == null || x.LastPlayDateTime < DateTime.Now.AddHours(-1 * randomizerIgnoreHours))).ToList();

                if (songsToChooseFrom.Count == 0)
                    songsToChooseFrom = _songs.Values.ToList();

                Song randomSong = songsToChooseFrom[random.Next(songsToChooseFrom.Count)];

                return new RequestedSong()
                {
                    Song = randomSong,
                    RequesterName = "randomizer"
                };
            }
        }

        /// <summary>
        /// Private class for containing some information when updating songs using thread pool
        /// </summary>
        private class SongLibrarySongUpdate
        {
            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="doneEvent"></param>
            public SongLibrarySongUpdate(ManualResetEvent doneEvent)
            {
                _doneEvent = doneEvent;
            }

            /// <summary>
            /// Reset event for the thread pool
            /// </summary>
            private ManualResetEvent _doneEvent;

            /// <summary>
            /// Callback method
            /// </summary>
            /// <param name="threadContext"></param>
            public void ThreadPoolCallback(object threadContext)
            {
                // get song
                Song song = (Song)threadContext;

                // update song
                UpdateSingleTag(song);

                // finish!
                _doneEvent.Set();
            }
        }
    }
}

