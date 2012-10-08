using System;
using System.Linq;
using System.Collections.Generic;
using WMPLib;


namespace SongRequest
{
    public class SongPlayerWindowsMediaPlayer : ISongplayer
    {
        private SongLibrary songLibrary;
        private WindowsMediaPlayer player = new WindowsMediaPlayer();

        private List<Song> _queue;
        private Song _currentSong;
        private DateTime _currentSongStart;

        public SongPlayerWindowsMediaPlayer()
        {
            songLibrary = new SongLibrary();
            songLibrary.ScanSongs("c:\\music");

            _queue = new List<Song>(songLibrary.GetSongs(string.Empty, 0, 3));
            player.PlayStateChange += new _WMPOCXEvents_PlayStateChangeEventHandler(player_PlayStateChange);

            Next();
        }

        void player_PlayStateChange(int NewState)
        {
            //1==stopped
            if (NewState == 1)
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
				_currentSong = songLibrary.GetRandomSong();
			}
					
			_currentSongStart = DateTime.Now;

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
                return songLibrary.GetSongs(string.Empty, 0, 100);
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

