using System;
using System.Linq;
using System.Collections.Generic;
using WMPLib;
using System.Threading;
using SongRequest.Config;


namespace SongRequest
{
    public class SongPlayerWindowsMediaPlayer : ISongplayer, IDisposable
    {
        private static object lockObject = new object();
        private SongLibrary _songLibrary;
        private WindowsMediaPlayer player;

        private FairQueue _queue;
        private RequestedSong _currentSong;
        private DateTime _currentSongStart;
        private Thread _updateThread;
        public event StatusChangedEventHandler LibraryStatusChanged;
        public event StatusChangedEventHandler PlayerStatusChanged;

        private volatile bool _running = true;

        public SongPlayerWindowsMediaPlayer()
        {
            player = new WindowsMediaPlayer();
            player.settings.volume = 75;

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

        public int Volume
        {
            get
            {
                try
                {
                    lock (lockObject)
                    {
                        return player.settings.volume;
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
                    lock (lockObject)
                    {
                        player.settings.volume = Math.Max(Math.Min(value, 100), 0);
                    }
                }
                catch
                {
                }

            }
        }

        public void Next(string requesterName)
        {
            if (!ClientAllowed(requesterName))
                return;


            if (_queue.Count > 0)
            {
                lock (lockObject)
                {
                    //Take next song from queue
                    _currentSong = _queue.Current.First();

                    _queue.Remove(_currentSong);
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
                        player.URL = _currentSong.Song.FileName;
                    }
                    catch
                    {
                        try
                        {
                            Thread.Sleep(50);
                            player.controls.stop();
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

        public void Update()
        {
            while (_running)
            {
                if (!_songLibrary.ScanLibrary())
                    Thread.Sleep(500);

                try
                {
                    WMPPlayState playState;
                    lock (lockObject)
                    {
                        playState = player.playState;
                    }

                    if (playState == WMPPlayState.wmppsStopped ||
                        playState == WMPPlayState.wmppsUndefined)
                        Next(null);
                }
                catch
                {
                }

                string status;
                if (SongPlayerFactory.GetSongPlayer().PlayerStatus.RequestedSong != null)
                    status = string.Format("Currently playing: {0} sec -> {1}", SongPlayerFactory.GetSongPlayer().PlayerStatus.Position, SongPlayerFactory.GetSongPlayer().PlayerStatus.RequestedSong.Song.GetArtistAndTitle());
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

            }
        }

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
                        playerStatus.Position = (int)player.controls.currentPosition;
                    }
                    catch
                    {
                        playerStatus.Position = (int)(DateTime.Now - _currentSongStart).TotalSeconds;
                    }
                }

                return playerStatus;
            }
        }

        public IEnumerable<Song> GetPlayList(string filter, string sortBy, bool ascending)
        {
            return _songLibrary.GetSongs(filter, sortBy, ascending);
        }

        public IEnumerable<RequestedSong> PlayQueue
        {
            get
            {
                return _queue.Current.ToArray();
            }
        }

        public void Enqueue(string id, string requesterName)
        {
            Song song = _songLibrary.GetSongs(string.Empty, null, true).FirstOrDefault(x => x.TempId == id);

            if (song != null)
            {
                Enqueue(song, requesterName);
            }
        }

        private bool ClientAllowed(string requesterName)
        {
            if (string.IsNullOrEmpty(requesterName))
                return true;

            string allowedClients = SongPlayerFactory.GetConfigFile().GetValue("server.clients");

            //Only allow clients from config file
            return string.IsNullOrEmpty(allowedClients) ||
                    allowedClients.Equals("all", StringComparison.OrdinalIgnoreCase) ||
                    SongPlayerFactory.GetConfigFile().GetValue("server.clients").ContainsOrdinalIgnoreCase(requesterName);
        }

        public void Enqueue(Song song, string requesterName)
        {
            if (!ClientAllowed(requesterName))
                return;

            int maximalsonginqueue;

            if (!int.TryParse(SongPlayerFactory.GetConfigFile().GetValue("player.maximalsonginqueue"), out maximalsonginqueue))
                maximalsonginqueue = int.MaxValue;

            if (_queue.Count >= maximalsonginqueue)
                return;

            SongLibrary.UpdateSingleTag(song);
            _queue.Add(new RequestedSong { Song = song, RequesterName = requesterName, RequestedDate = DateTime.Now });
        }

        public void Dequeue(string id, string requesterName)
        {
            Song song = _songLibrary.GetSongs(string.Empty, null, true).FirstOrDefault(x => x.TempId == id);

            if (song != null)
                Dequeue(song, requesterName);
        }

        public void Dequeue(Song song, string requesterName)
        {
            if (!ClientAllowed(requesterName))
                return;

            _queue.Remove(song.TempId);
        }

        public void Pause(string requesterName)
        {
            if (!ClientAllowed(requesterName))
                return;

            lock (lockObject)
            {
                if (player.playState == WMPPlayState.wmppsPaused)
                    player.controls.play();
                else
                    player.controls.pause();
            }
        }

        public void Rescan(string requesterName)
        {
            if (!ClientAllowed(requesterName))
                return;

            _songLibrary.Rescan();
        }

        public void Dispose()
        {
            _running = false;
        }
    }
}

