using System;
using System.Linq;
using System.Collections.Generic;
using WMPLib;


namespace SongRequest
{
    public class SongPlayerWindowsMediaPlayer : ISongplayer
    {
        private SongLibrary _songLibrary;
        private WindowsMediaPlayer player = new WindowsMediaPlayer();

        private List<Song> _queue;
        private Song _currentSong;
        private DateTime _currentSongStart;

        public SongPlayerWindowsMediaPlayer()
        {
            _queue = new List<Song>();
            _songLibrary = new SongLibrary();
            _songLibrary.ScanSongs("c:\\music");
            player.PlayStateChange += new _WMPOCXEvents_PlayStateChangeEventHandler(player_PlayStateChange);

            Next();
        }

        void player_PlayStateChange(int NewState)
        {
            //8==mediaended
            if (NewState == 8)
            {
                Next();
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
            player.URL = string.Empty; //first clear filename -> needed when the same file is played twice in a row
            player.URL = _currentSong.FileName;
        }

        public PlayerStatus PlayerStatus
        {
            get
            {
                PlayerStatus playerStatus = new PlayerStatus();
                playerStatus.Song = _currentSong;
                playerStatus.Position = DateTime.Now - _currentSongStart;

                return playerStatus;
            }
        }

        public IEnumerable<Song> PlayList
        {
            get
            {
                return _songLibrary.GetSongs(string.Empty, 0, 100);
            }
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
    }
}

