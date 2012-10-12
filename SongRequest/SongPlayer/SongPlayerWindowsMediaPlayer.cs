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
                lock (lockObject)
                {
                    return player.settings.volume;
                }
            }
            set
            {
                lock (lockObject)
                {
                    player.settings.volume = Math.Max(Math.Min(value, 100), 0);
                }
            }
        }

        public void Next(string requesterName)
        {
            if (!ClientAllowed(requesterName))
                return;

            if (_queue.Count > 0)
            {
                //Take next song from queue
                _currentSong = _queue.Current.First();

                _queue.Remove(_currentSong);
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
                    player.URL = _currentSong.Song.FileName;
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
                    status = string.Format("Currently playing: {0} sec - {1}", SongPlayerFactory.GetSongPlayer().PlayerStatus.Position, SongPlayerFactory.GetSongPlayer().PlayerStatus.RequestedSong.Song.Name);
                else
                    status = "No song playing...";

                OnPlayerStatusChanged(status);

                //Enqueue random song when the queue is empty and the current song is almost finished
                if (_queue.Count == 0 &&  _currentSong != null && (int)(DateTime.Now - _currentSongStart).TotalSeconds + 20 > _currentSong.Song.Duration)
                {
                    RequestedSong requestedSong = _songLibrary.GetRandomSong();
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
                playerStatus.Position = (int)(DateTime.Now - _currentSongStart).TotalSeconds;
                playerStatus.Volume = this.Volume;

                return playerStatus;
            }
        }

        public IEnumerable<Song> GetPlayList(string filter)
        {
            return _songLibrary.GetSongs(filter);
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
            Song song = _songLibrary.GetSongs(string.Empty).FirstOrDefault(x => x.TempId == id);

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
            return  string.IsNullOrEmpty(allowedClients) ||
                    allowedClients.Equals("all", StringComparison.OrdinalIgnoreCase) ||
                    SongPlayerFactory.GetConfigFile().GetValue("server.clients").ToLower().Contains(requesterName);
        }

        public void Enqueue(Song song, string requesterName)
        {
            if (!ClientAllowed(requesterName))
                return;

            SongLibrary.UpdateSingleTag(song);
            _queue.Add(new RequestedSong { Song = song, RequesterName = requesterName, RequestedDate = DateTime.Now });
        }

        public void Dequeue(string id, string requesterName)
        {
            Song song = _songLibrary.GetSongs(string.Empty).FirstOrDefault(x => x.TempId == id);

            if (song != null)
                Dequeue(song, requesterName);
        }

        public void Dequeue(Song song, string requesterName)
        {
            if (!ClientAllowed(requesterName))
                return;

            _queue.Remove(song.TempId);
        }

        public void Dispose()
        {
            _running = false;
        }
    }
}

