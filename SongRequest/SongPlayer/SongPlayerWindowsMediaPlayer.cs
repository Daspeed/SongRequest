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
        private SongLibrary _songLibrary;
        private WindowsMediaPlayer player;

        private List<RequestedSong> _queue;
        private RequestedSong _currentSong;
        private DateTime _currentSongStart;
        private Thread _updateThread;
        public event StatusChangedEventHandler LibraryStatusChanged;
        public event StatusChangedEventHandler PlayerStatusChanged;

        private volatile bool _running = true;

        public SongPlayerWindowsMediaPlayer()
        {
            player = new WindowsMediaPlayer();
            player.settings.volume = 10;
            _queue = new List<RequestedSong>();
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
                player.URL = _currentSong.Song.FileName;
            }
        }

        public void Update()
        {
            while (_running)
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
                if (SongPlayerFactory.GetSongPlayer().PlayerStatus.RequestedSong != null)
                    status = string.Format("Currently playing: {0} sec - {1}", SongPlayerFactory.GetSongPlayer().PlayerStatus.Position, SongPlayerFactory.GetSongPlayer().PlayerStatus.RequestedSong.Song.Name);
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
                playerStatus.RequestedSong = _currentSong;
                playerStatus.Position = (int)(DateTime.Now - _currentSongStart).TotalSeconds;
                playerStatus.Volume = this.Volume;

                return playerStatus;
            }
        }

        public IEnumerable<Song> GetPlayList(string filter, int skip, int count)
        {
            return _songLibrary.GetSongs(filter, skip, count);
        }

        public IEnumerable<RequestedSong> PlayQueue
        {
            get
            {
                return _queue;
            }
        }

        public void Enqueue(long id, string requesterName)
        {
            Song song = _songLibrary.GetSongs(string.Empty, 0, int.MaxValue).FirstOrDefault(x => x.TempId == id);
            
            if(song != null)
                Enqueue(song, requesterName);
        }

        public void Enqueue(Song song, string requesterName)
        {
            _queue.Add(new RequestedSong { Song = song, RequesterName = requesterName });
        }

        public void Dequeue(long id)
        {
            Song song = _songLibrary.GetSongs(string.Empty, 0, int.MaxValue).FirstOrDefault(x => x.TempId == id);

            if (song != null)
                Dequeue(song);
        }

        public void Dequeue(Song song)
        {
            bool found = false;
            _queue.RemoveAll(x => {
                if (x.Song == song && !found)
                {
                    found = true;
                    return true;
                }
                return false;
            });
        }

        public void Dispose()
        {
            _running = false;
        }
    }
}

