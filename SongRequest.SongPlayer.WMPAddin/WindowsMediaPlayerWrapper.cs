using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using WMPLib;

namespace SongRequest.SongPlayer.WMPAddin
{
    [Export(typeof(IMediaDevice))]
    public class WindowsMediaPlayerWrapper : IMediaDevice
    {
        private WindowsMediaPlayer _player;

        public WindowsMediaPlayerWrapper()
        {
            _player = new WindowsMediaPlayer();
        }
        
        public void Pause()
        {
            if (_player.playState == WMPPlayState.wmppsPaused)
                _player.controls.play();
            else
                _player.controls.pause();
        }

        public bool IsPlaying
        {
            get
            {
                WMPPlayState playState = _player.playState;
                return playState != WMPPlayState.wmppsStopped && playState != WMPPlayState.wmppsUndefined;
            }
        }

        public void PlaySong(string name)
        {
            _player.URL = name;
        }

        public long Position
        {
            get
            {
                return (int)_player.controls.currentPosition;
            }
        }

        public void Stop()
        {
            _player.controls.stop();
        }

        public int Volume
        {
            get
            {
                return _player.settings.volume;
            }
            set
            {
                _player.settings.volume = value;
            }
        }
    }
}
