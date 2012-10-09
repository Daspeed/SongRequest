using System;
using SongRequest.Config;

namespace SongRequest
{
	public static class SongPlayerFactory
	{
		private static ISongplayer _songPlayer;

		public static ISongplayer CreateSongPlayer()
		{
            if (_songPlayer == null)
            {
                //_songPlayer = new SongPlayerMock();
                _songPlayer = new SongPlayerWindowsMediaPlayer();
            }
			
			return _songPlayer;
		}

        public static ConfigFile GetConfigFile()
        {
            if (!System.IO.File.Exists("songrequest.config"))
            {
                ConfigFile configFile = new ConfigFile("songrequest.config");
                configFile.SetValue("server.port", "8765");
                configFile.SetValue("library.path", "c:\\music");
                configFile.SetValue("library.minutesbetweenscans", "1");
                configFile.Save();
            }

            return new ConfigFile("songrequest.config");
        }
	}
}

