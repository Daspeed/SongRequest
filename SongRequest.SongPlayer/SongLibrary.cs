using DoubleMetaphone;
using SongRequest.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SongRequest.SongPlayer
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

        public HashSet<string> GetTempIds()
        {
            HashSet<string> values = new HashSet<string>();
            lock (lockObject)
            {
                if (_songs != null)
                    values.UnionWith(_songs.Select(x => x.Value.TempId));
            }
            return values;
        }

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

            bool tagChanges = UpdateTags();
            if (tagChanges)
            {
                int noTagCount;
                int songCount;
                lock (lockObject)
                {
                    noTagCount = _songs.Values.Count(s => s.TagRead);
                    songCount = _songs.Count();
                }
                OnStatusChanged(string.Format("Library updated: {0} songs. Tags read: {1}/{0}. Next scan: {2}.", songCount, noTagCount, (_lastFullUpdate + TimeSpan.FromMinutes(minutesBetweenScans)).ToShortTimeString()));
            }
            else
            {
                int songCount;
                lock (lockObject)
                {
                    songCount = _songs.Count();
                }
                OnStatusChanged("Library update completed (" + songCount + " songs). Next scan: " + (_lastFullUpdate + TimeSpan.FromMinutes(minutesBetweenScans)).ToShortTimeString());
            }

            // check need to save
            // -> unsaved changes
            // -> tag(s) are changed
            // -> song is marked dirty (last time played is changed)
            bool dirtySongs;
            lock (lockObject)
            {
                dirtySongs = _songs.Values.Any(x => x.IsDirty);
            }
            _unsavedChanges = _unsavedChanges || tagChanges || dirtySongs;

            //Save, but no more than once every 2 minutes
            if (_unsavedChanges && _lastSerialize + TimeSpan.FromMinutes(2) < DateTime.Now)
                Serialize();

            //No need to scan...
            if (_lastFullUpdate + TimeSpan.FromMinutes(minutesBetweenScans) > DateTime.Now)
                return tagChanges;

            bool fileChanges = ScanSongs();
            if (fileChanges || tagChanges)
            {
                int songCount;
                int noTagCount;
                lock (lockObject)
                {
                    songCount = _songs.Count();
                    noTagCount = _songs.Values.Count(s => s.TagRead);
                }
                OnStatusChanged(string.Format("Library updated: {0} songs. Tags read: {1}/{0}. Next scan: {2}.", songCount, noTagCount, (_lastFullUpdate + TimeSpan.FromMinutes(minutesBetweenScans)).ToShortTimeString()));
            }

            _lastFullUpdate = DateTime.Now;

            if (!fileChanges || !tagChanges)
            {
                int songCount;
                lock (lockObject)
                {
                    songCount = _songs.Count();
                }
                OnStatusChanged("Library update completed (" + songCount + " songs). Next scan: " + (_lastFullUpdate + TimeSpan.FromMinutes(minutesBetweenScans)).ToShortTimeString());
            }

            _unsavedChanges = _unsavedChanges || fileChanges || tagChanges;

            return fileChanges || tagChanges;
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
                        bin.Serialize(stream, _songs.Values.ToList());
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
                                object fromLibrary = bin.Deserialize(stream);

                                if (fromLibrary is List<Song>)
                                {
                                    List<Song> songs = (List<Song>)fromLibrary;
                                    foreach (Song song in songs)
                                    {
                                        song.GenerateSearchAndDoubleMetaphone();
                                        _songs.Add(song.FileName, song);
                                    }
                                }
                                else if (fromLibrary is Dictionary<string, Song>)
                                {
                                    _songs = (Dictionary<string, Song>)fromLibrary;

                                    foreach (Song song in _songs.Values)
                                    {
                                        song.GenerateSearchAndDoubleMetaphone();
                                    }
                                }
                                else if (fromLibrary is Dictionary<string, Song>.ValueCollection)
                                {
                                    Dictionary<string, Song>.ValueCollection valueCollection = (Dictionary<string, Song>.ValueCollection)fromLibrary;

                                    List<Song> songs = valueCollection.ToList();
                                    foreach (Song song in songs)
                                    {
                                        song.GenerateSearchAndDoubleMetaphone();
                                        _songs.Add(song.FileName, song);
                                    }
                                }
                                else
                                {
                                    throw new Exception(string.Format("Songs saved in unknown type '{0}'!", fromLibrary.GetType().Name));
                                }

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
            catch (Exception)
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

        private bool ScanSongs()
        {
            if (!_scanRunning)
            {
                _scanFoundChange = false;
                _scanRunning = true;
                Thread scanThread = new Thread(new ThreadStart(ScanSongsThread));

                scanThread.Start();
            }

            return _scanFoundChange;
        }

        public bool ScanRunning { get { return _scanRunning; } }
        private volatile bool _scanRunning = false;
        private volatile bool _scanFoundChange = false;
        private void ScanSongsThread()
        {
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
                string[] keysToRemove = _songs.Keys.Where(x => !files.Contains(x, StringComparer.OrdinalIgnoreCase)).ToArray();

                foreach (string key in keysToRemove)
                {
                    if (_songs.Remove(key))
                    {
                        _scanFoundChange = true;
                    }
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
                    song.GenerateSearchAndDoubleMetaphone();

                    lock (lockObject)
                    {
                        _songs.Add(fileName, song);
                    }

                    _scanFoundChange = true;
                }
            });

            _scanRunning = false;
        }

        private bool UpdateTags()
        {
            // return false, not updating
            if (_songs.Count == 0)
                return false;

            if (!_updateRunning)
            {
                _updatedTag = false;
                _updateRunning = true;
                Thread updateThread = new Thread(new ThreadStart(UpdateTagsThread));

                updateThread.Start();
            }

            // if running, return true
            return _updatedTag;
        }

        volatile bool _updateRunning = false;
        volatile bool _updatedTag = false;
        private void UpdateTagsThread()
        {
            Song song;

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

                    // update song
                    UpdateSingleTag(song);

                    _updatedTag = true;
                }

                // loop until no song found
            } while (song != null);

            // finished this run
            _updateRunning = false;
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

                        if (!string.IsNullOrEmpty(taglibFile.Tag.Album))
                            song.Album = taglibFile.Tag.Album.Trim();

                        int rating = int.MinValue;
                        if (taglibFile.Tag.TagTypes.HasFlag(TagLib.TagTypes.Id3v2))
                        {
                            TagLib.Id3v2.Tag id3v2Tag = (TagLib.Id3v2.Tag)taglibFile.GetTag(TagLib.TagTypes.Id3v2);

                            List<TagLib.Id3v2.PopularimeterFrame> popularimeterFrames = id3v2Tag
                                .Where(x => x is TagLib.Id3v2.PopularimeterFrame)
                                .Cast<TagLib.Id3v2.PopularimeterFrame>()
                                .OrderBy(y => y.User)
                                .ToList();

                            if (popularimeterFrames != null && popularimeterFrames.Count > 0)
                            {
                                foreach (TagLib.Id3v2.PopularimeterFrame popularimeterFrame in popularimeterFrames)
                                {
                                    if (popularimeterFrame.Rating > 0)
                                    {
                                        rating = popularimeterFrame.Rating;
                                        break;
                                    }
                                }
                            }
                        }

                        if (rating == 0)
                            song.Rating = 1;
                        else if (rating == 1)
                            song.Rating = 2;
                        else if (rating > 1 && rating < 64)
                            song.Rating = 3;
                        else if (rating == 64)
                            song.Rating = 4;
                        else if (rating > 64 && rating < 128)
                            song.Rating = 5;
                        else if (rating == 128)
                            song.Rating = 6;
                        else if (rating > 128 && rating < 196)
                            song.Rating = 7;
                        else if (rating == 196)
                            song.Rating = 8;
                        else if (rating > 196 && rating < 255)
                            song.Rating = 9;
                        else if (rating == 255)
                            song.Rating = 10;
                        else
                            song.Rating = -1;

                        song.Duration = (int)taglibFile.Properties.Duration.TotalSeconds;

                        uint year = taglibFile.Tag.Year;
                        song.Year = year > 0 ? year.ToString() : string.Empty;

                        FileInfo fileInfo = new FileInfo(song.FileName);
                        song.DateCreated = fileInfo.CreationTime.ToString("yyyy-MM-dd HH:mm");

                        song.GenerateSearchAndDoubleMetaphone();
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

        public Song GetSong(string tempId)
        {
            lock (lockObject)
            {
                return _songs.FirstOrDefault(x => x.Value.TempId.Equals(tempId)).Value;
            }
        }

        public MemoryStream GetImageStream(string tempId, bool large)
        {
            Song song = GetSong(tempId);

            if (song == null)
                return null;

            return song.GetImageStream(large);
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

                    string searchValue = includeFileNameInSearch ? match.Groups[2].Value : filter;
                    Func<string, bool> searchFunc = (source) => StringExtensions.ContainsIgnoreCaseNonSpace(source, searchValue);

                    Regex regex = null;
                    if (match.Success && match.Groups[1].Value.Contains("r"))
                    {
                        regex = new Func<Regex>(() =>
                        {
                            try { return new Regex(match.Groups[2].Value, RegexOptions.IgnoreCase); }
                            catch (Exception) { return null; }
                        })();

                        if (regex != null)
                            searchFunc = regex.IsMatch;
                    }

                    if (regex != null)
                    {
                        songs = _songs.AsParallel().Where(s =>
                            searchFunc(s.Value.Name ?? string.Empty) ||
                            searchFunc(s.Value.Artist ?? string.Empty) ||
                            (includeFileNameInSearch ? searchFunc(s.Key ?? string.Empty) : false)
                        ).Select(x => x.Value);
                    }
                    else
                    {
                        string betterSearchValue = searchValue.ToLower().ReplaceUniqueCharacters();
                        string searchDoubleMetaphone = betterSearchValue.GenerateDoubleMetaphone();
                        songs = _songs.AsParallel().Where(s => SearchFunction(s.Value, betterSearchValue, searchDoubleMetaphone, includeFileNameInSearch)).Select(x => x.Value);
                    }
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
                else if (sortBy.Equals("rating", StringComparison.OrdinalIgnoreCase))
                    importantSort = SortBy.Rating;
                else if (sortBy.Equals("album", StringComparison.OrdinalIgnoreCase))
                    importantSort = SortBy.Album;
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

        private bool SearchFunction(Song song, string searchValue, string searchValueDoubleMetaphone, bool includeFileNameInSearch)
        {
            // with double metaphone
            if (!string.IsNullOrEmpty(song.NameDoubleMetaphone) && song.NameDoubleMetaphone.Equals(searchValueDoubleMetaphone, StringComparison.Ordinal))
                return true;
            if (!string.IsNullOrEmpty(song.ArtistDoubleMetaphone) && song.ArtistDoubleMetaphone.Equals(searchValueDoubleMetaphone, StringComparison.Ordinal))
                return true;
            if (!string.IsNullOrEmpty(song.AlbumDoubleMetaphone) && song.AlbumDoubleMetaphone.Equals(searchValueDoubleMetaphone, StringComparison.Ordinal))
                return true;

            // contains
            if (!string.IsNullOrEmpty(song.NameSearchValue) && song.NameSearchValue.ContainsIgnoreCaseNonSpace(searchValue))
                return true;
            if (!string.IsNullOrEmpty(song.ArtistSearchValue) && song.ArtistSearchValue.ContainsIgnoreCaseNonSpace(searchValue))
                return true;
            if (!string.IsNullOrEmpty(song.AlbumSearchValue) && song.AlbumSearchValue.ContainsIgnoreCaseNonSpace(searchValue))
                return true;

            // file name?
            if (includeFileNameInSearch && !string.IsNullOrEmpty(song.FileNameSearchValue) && song.FileNameSearchValue.ContainsIgnoreCaseNonSpace(searchValue))
                return true;

            return false;
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
            else if (sortBy == SortBy.Rating)
            {
                switch (level)
                {
                    case 0:
                        return song.Rating.ToString("00");
                    case 1:
                        return song.Name;
                    case 2:
                        return song.Artist;
                }
            }
            else if (sortBy == SortBy.Album)
            {
                switch (level)
                {
                    case 0:
                        return song.Album;
                    case 1:
                        return song.Name;
                    case 2:
                        return song.Artist;
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
            Year,
            Rating,
            Album
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
    }
}

