using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.ComponentModel.Composition;
using SongRequest.SongPlayer.VlcPlayer;

namespace SongRequest.SongPlayer
{
    [Export(typeof(ISongplayer))]
    public class SongPlayer : ISongplayer, IDisposable
    {
        private static object lockObject = new object();
        private SongLibrary _songLibrary;

        private VlcWrapper player;

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
        public SongPlayer()
        {

            player = new VlcWrapper();
            
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
                        return player.Volume;
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
                    if (minimumVolume > 100)
                        minimumVolume = 100;
                    if (minimumVolume < 0)
                        minimumVolume = 0;

                    // check maximum volume
                    int maximumVolume;
                    if (!int.TryParse(SongPlayerFactory.GetConfigFile().GetValue("player.maximumvolume"), out maximumVolume))
                        maximumVolume = 100;
                    if (maximumVolume > 100)
                        maximumVolume = 100;
                    if (maximumVolume < 1)
                        maximumVolume = 1;

                    lock (lockObject)
                    {
                        player.Volume = Math.Max(Math.Min(value, maximumVolume), minimumVolume);
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
                        player.PlaySong(_currentSong.Song.FileName);
                        _currentSong.Song.LastRequester = _currentSong.RequesterName.Equals("randomizer", StringComparison.OrdinalIgnoreCase) ? string.Empty : _currentSong.RequesterName;
                        _currentSong.Song.SkippedBy = string.Empty;
                        _currentSong.Song.LastPlayDateTime = DateTime.Now;
                    }
                    catch
                    {
                        try
                        {
                            Thread.Sleep(50);
                            player.Stop();
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
                    bool playState;
                    lock (lockObject)
                    {
                        playState = player.Playing; 
                    }

                    if (!playState)
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

                if (!_songLibrary.ScanLibrary())
                    Thread.Sleep(500);
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
                        playerStatus.Position = (int)player.Position/1000; 
                    }
                    catch
                    {
                        playerStatus.Position = (int)(DateTime.Now - _currentSongStart).TotalSeconds;
                    }
                }

                return playerStatus;
            }
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
            Song song = _songLibrary.GetSongs(string.Empty, null, true).FirstOrDefault(x => x.TempId == id);

            if (song != null)
                Dequeue(song, requester);
        }

        /// <summary>
        /// Dequeue song
        /// </summary>
        public void Dequeue(Song song, string requester)
        {
            _queue.Remove(song.TempId, requester, _currentSong.Song.TempId);
        }

        /// <summary>
        /// Pause the player
        /// </summary>
        public void Pause()
        {
            lock (lockObject)
            {
                player.Pause();
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

