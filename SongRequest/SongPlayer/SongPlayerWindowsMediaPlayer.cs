using System;
using System.Linq;
using System.Collections.Generic;
using WMPLib;
using System.Threading;


namespace SongRequest
{
    public class SongPlayerWindowsMediaPlayer : ISongplayer, IDisposable
    {
        private SongLibrary _songLibrary;
        private WindowsMediaPlayer player;

        private List<Song> _queue;
        private Song _currentSong;
        private DateTime _currentSongStart;
        private Thread _updateThread;
        public event StatusChangedEventHandler LibraryStatusChanged;
        public event StatusChangedEventHandler PlayerStatusChanged;


        public SongPlayerWindowsMediaPlayer()
        {
            player = new WindowsMediaPlayer();
            player.settings.volume = 10;
            _queue = new List<Song>();
            _songLibrary = new SongLibrary("c:\\music");
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
                return player.settings.volume;
            }
            set
            {
                player.settings.volume = Math.Max(Math.Min(value, 100), 0);
            }
        }

        public void Next()
        {
			if (_queue.Count > 0)
			{					
				//Take next song from queue
				_currentSong = _queue[0];
						
				_queue.Remove(_currentSong);    
			} else
			{
				//Take random song
				_currentSong = _songLibrary.GetRandomSong();
			}
					
			_currentSongStart = DateTime.Now;

            if (_currentSong != null)
            {
                player.URL = _currentSong.FileName;
            }
        }

        public void Update()
        {
            while (true)
            {
                _songLibrary.ScanLibrary();

                try
                {
                    if (player.playState == WMPPlayState.wmppsStopped ||
                        player.playState == WMPPlayState.wmppsUndefined)
                        Next();
                }
                catch
                {
                }

                string status;
                if (SongPlayerFactory.CreateSongPlayer().PlayerStatus.Song != null)
                    status = string.Format("Currently playing: {0} - {1}", SongPlayerFactory.CreateSongPlayer().PlayerStatus.Position, SongPlayerFactory.CreateSongPlayer().PlayerStatus.Song.FileName);
                else
                    status = "No song playing...";

                OnPlayerStatusChanged(status);

                Thread.Sleep(100);
            }
        }

        public PlayerStatus PlayerStatus
        {
            get
            {
                PlayerStatus playerStatus = new PlayerStatus();
                playerStatus.Song = _currentSong;
                playerStatus.Position = (int)(DateTime.Now - _currentSongStart).TotalSeconds;
                playerStatus.Volume = this.Volume;

                return playerStatus;
            }
        }

        public IEnumerable<Song> GetPlayList(string filter, int skip, int count)
        {
            return _songLibrary.GetSongs(filter, skip, count);
        }

        public IEnumerable<Song> PlayQueue
        {
            get
            {
                return _queue;
            }
        }

        public void Enqueue(Song song)
        {
            _queue.Add(song);
        }

        public void Dequeue(Song song)
        {
            _queue.Remove(song);
        }

        public void Dispose()
        {
            if (_updateThread != null &&
                _updateThread.IsAlive)
            {
                _updateThread.Abort();
            }
        }
    }
}

