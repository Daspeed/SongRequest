using System;
using System.Linq;
using System.Collections.Generic;
using WMPLib;


namespace SongRequest
{
    public class SongPlayerWindowsMediaPlayer : ISongplayer
    {
        private SongLibrary _songLibrary;
        private WindowsMediaPlayer player;

        private List<Song> _queue;
        private Song _currentSong;
        private DateTime _currentSongStart;

        public SongPlayerWindowsMediaPlayer()
        {
            player = new WindowsMediaPlayer();
            _queue = new List<Song>();
            _songLibrary = new SongLibrary();
            _songLibrary.ScanSongs("c:\\music");
            
            Update();
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
            if (player.playState != WMPPlayState.wmppsPlaying)
                Next();
        }

        public PlayerStatus PlayerStatus
        {
            get
            {
                //TODO: Find a better place for this... should be called as often as possible
                Update();

                PlayerStatus playerStatus = new PlayerStatus();
                playerStatus.Song = _currentSong;
                playerStatus.Position = (int)(DateTime.Now - _currentSongStart).TotalSeconds;

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
    }
}
