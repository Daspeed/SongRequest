using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace SongRequest.SongPlayer
{
    public class SongPlayer : ISongPlayer, IDisposable
    {
        private static object lockObject = new object();
        private SongLibrary _songLibrary;
        private IMediaDevice _mediaDevice;

        private FairQueue _queue;
        private RequestedSong _currentSong;
        private DateTime _currentSongStart;
        private Thread _updateThread;
        public event StatusChangedEventHandler LibraryStatusChanged;
        public event StatusChangedEventHandler PlayerStatusChanged;

        private volatile bool _running = true;

        /// <summary>
        /// Constructor
        /// </summary>
        public SongPlayer(IMediaDevice mediaDevice)
        {
            _mediaDevice = mediaDevice;

            _queue = new FairQueue();
            _songLibrary = new SongLibrary();
            _songLibrary.StatusChanged += OnLibraryStatusChanged;

            _updateThread = new Thread(new ThreadStart(Update));
            _updateThread.Start();

        }

        protected virtual void OnLibraryStatusChanged(string status)
        {
            if (LibraryStatusChanged != null)
                LibraryStatusChanged(status);
        }

        protected virtual void OnPlayerStatusChanged(string status)
        {
            if (PlayerStatusChanged != null)
                PlayerStatusChanged(status);
        }

        /// <summary>
        /// Player's volume
        /// </summary>
        public int Volume
        {
            get
            {
                try
                {
                    lock (lockObject)
                    {
                        return _mediaDevice.Volume;
                    }
                }
                catch
                {
                    return 0;
                }
            }
            set
            {
                try
                {
                    int minimumVolume;
                    if (!int.TryParse(SongPlayerFactory.GetConfigFile().GetValue("player.minimumvolume"), out minimumVolume))
                        minimumVolume = 0;

                    minimumVolume = Math.Min(100, Math.Max(0, minimumVolume));

                    int maximumVolume;
                    if (!int.TryParse(SongPlayerFactory.GetConfigFile().GetValue("player.maximumvolume"), out maximumVolume))
                        maximumVolume = 100;

                    maximumVolume = Math.Min(100, Math.Max(0, maximumVolume));

                    lock (lockObject)
                    {
                        _mediaDevice.Volume = Math.Max(Math.Min(value, maximumVolume), minimumVolume);
                    }
                }
                catch
                {
                }
            }
        }

        /// <summary>
        /// Play next song
        /// </summary>
        public void Next(string requester)
        {
            if (_currentSong != null)
                _currentSong.Song.SkippedBy = requester;

            if (_queue.Count > 0)
            {
                lock (lockObject)
                {
                    //Take next song from queue
                    _currentSong = _queue.Current.First();

                    _queue.Remove(_currentSong, requester, _currentSong.Song.TempId);
                }
            }
            else
            {
                //Take random song
                _currentSong = _songLibrary.GetRandomSong();
            }

            _currentSongStart = DateTime.Now;

            if (_currentSong != null)
            {
                lock (lockObject)
                {
                    try
                    {
                        _mediaDevice.PlaySong(_currentSong.Song.FileName);
                        _currentSong.Song.LastRequester = _currentSong.RequesterName.Equals("randomizer", StringComparison.OrdinalIgnoreCase) ? string.Empty : _currentSong.RequesterName;
                        _currentSong.Song.SkippedBy = string.Empty;
                        _currentSong.Song.LastPlayDateTime = DateTime.Now;
                    }
                    catch
                    {
                        try
                        {
                            Thread.Sleep(50);
                            _mediaDevice.Stop();
                            //Try to stop the player... if this fails, just ignore...
                        }
                        catch
                        {
                            //ignore this
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Update method
        /// </summary>
        public void Update()
        {
            while (_running)
            {
                try
                {
                    bool isPlaying;
                    lock (lockObject)
                    {
                        isPlaying = _mediaDevice.IsPlaying;
                    }

                    if (!isPlaying)
                        Next("randomizer");
                }
                catch
                {
                }

                string status;
                if (SongPlayerFactory.GetSongPlayer().PlayerStatus.RequestedSong != null)
                    status = string.Format("Currently playing at volume {2}: {0} sec -> {1}", SongPlayerFactory.GetSongPlayer().PlayerStatus.Position, SongPlayerFactory.GetSongPlayer().PlayerStatus.RequestedSong.Song.GetArtistAndTitle(), SongPlayerFactory.GetSongPlayer().Volume);
                else
                    status = "No song playing...";

                OnPlayerStatusChanged(status);

                int minimalsonginqueue;

                if (!int.TryParse(SongPlayerFactory.GetConfigFile().GetValue("player.minimalsonginqueue"), out minimalsonginqueue))
                    minimalsonginqueue = 0;

                //Enqueue random song when the queue is empty and the current song is almost finished
                if (_currentSong != null && _queue.Count < minimalsonginqueue + ((int)(DateTime.Now - _currentSongStart).TotalSeconds + 20 > _currentSong.Song.Duration ? 1 : 0))
                {
                    RequestedSong requestedSong = _songLibrary.GetRandomSong();
                    if (requestedSong != null)
                        Enqueue(requestedSong.Song, requestedSong.RequesterName);
                }

                // when not scanning for songs, clear the queue of unavailable songs
                if (!_songLibrary.ScanRunning)
                {
                    ClearQueue();
                }

                if (!_songLibrary.ScanLibrary())
                {
                    Thread.Sleep(50);
                }
                else
                {
                    Thread.Sleep(10);
                }
            }
        }

        /// <summary>
        /// Clears queue of unavailable songs
        /// </summary>
        private void ClearQueue()
        {
            // remove songs from queue that aren't available anymore
            if (_queue.Count > 0)
            {
                // all songs
                HashSet<string> allTempIds = _songLibrary.GetTempIds();

                // songs in queue
                HashSet<string> queueTempIds;
                lock (lockObject)
                {
                    queueTempIds = new HashSet<string>(_queue.Current.Select(x => x.Song.TempId));
                }

                // remove available ids
                queueTempIds.ExceptWith(allTempIds);

                // if any, remove them
                if (queueTempIds.Count > 0)
                {
                    foreach (string idToRemove in queueTempIds)
                    {
                        _queue.Remove(idToRemove, "randomizer", null);
                    }
                }
            }
        }

        /// <summary>
        /// Current player status
        /// </summary>
        public PlayerStatus PlayerStatus
        {
            get
            {
                PlayerStatus playerStatus = new PlayerStatus();
                playerStatus.RequestedSong = _currentSong;
                playerStatus.Volume = this.Volume;

                lock (lockObject)
                {
                    try
                    {
                        playerStatus.Position = (int)_mediaDevice.Position;
                    }
                    catch
                    {
                        playerStatus.Position = (int)(DateTime.Now - _currentSongStart).TotalSeconds;
                    }
                }

                return playerStatus;
            }
        }

        public MemoryStream GetImageStream(string tempId, bool large)
        {
            return _songLibrary.GetImageStream(tempId, large);
        }

        /// <summary>
        /// Get playlist
        /// </summary>
        public IEnumerable<Song> GetPlayList(string filter, string sortBy, bool ascending)
        {
            return _songLibrary.GetSongs(filter, sortBy, ascending);
        }

        /// <summary>
        /// Play queue
        /// </summary>
        public IEnumerable<RequestedSong> PlayQueue
        {
            get
            {
                return _queue.Current.ToArray();
            }
        }

        /// <summary>
        /// Enqueue song
        /// </summary>
        public void Enqueue(string id, string requesterName)
        {
            Song song = _songLibrary.GetSongs(string.Empty, null, true).FirstOrDefault(x => x.TempId == id);

            if (song != null)
            {
                Enqueue(song, requesterName);
            }
        }

        /// <summary>
        /// Enqueue song
        /// </summary>
        public void Enqueue(Song song, string requesterName)
        {
            int maximalsonginqueue;

            if (!int.TryParse(SongPlayerFactory.GetConfigFile().GetValue("player.maximalsonginqueue"), out maximalsonginqueue))
                maximalsonginqueue = int.MaxValue;

            if (_queue.Count >= maximalsonginqueue)
                return;

            SongLibrary.UpdateSingleTag(song);

            _queue.Add(new RequestedSong
            {
                Song = song,
                RequesterName = requesterName,
                RequestedDate = DateTime.Now
            });
        }

        /// <summary>
        /// Dequeue song
        /// </summary>
        public void Dequeue(string id, string requester)
        {
            _queue.Remove(id, requester, _currentSong.Song.TempId);
        }

        /// <summary>
        /// Dequeue song
        /// </summary>
        public void Dequeue(Song song, string requester)
        {
            Dequeue(song.TempId, requester);
        }

        /// <summary>
        /// Pause the player
        /// </summary>
        public void Pause()
        {
            lock (lockObject)
            {
                _mediaDevice.Pause();
            }
        }

        /// <summary>
        /// Rescan complete library
        /// </summary>
        public void Rescan()
        {
            _songLibrary.Rescan();
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            _running = false;
        }
    }
}

