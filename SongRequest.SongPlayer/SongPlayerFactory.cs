using System.ComponentModel.Composition;
using SongRequest.Config;

namespace SongRequest.SongPlayer
{
    public static class SongPlayerFactory
    {
        [Import(typeof(ISongplayer))]
        private static ISongplayer _songPlayer;

        public static ISongplayer GetSongPlayer()
        {
            //TODO: check for null here
            return _songPlayer;
        }

        public static ConfigFile GetConfigFile()
        {
            if (!System.IO.File.Exists("songrequest.config"))
            {
                ConfigFile configFile = new ConfigFile("songrequest.config");
                configFile.SetValue("server.port", "8765");
                configFile.SetValue("server.clients", "all");
                configFile.SetValue("library.path", "c:\\music");
                configFile.SetValue("library.minutesbetweenscans", "1");
                configFile.SetValue("library.extensions", "mp3");
                configFile.SetValue("player.minimalsonginqueue", "0");
                configFile.SetValue("player.maximalsonginqueue", "500");
                configFile.SetValue("player.startupvolume", "50");
                configFile.Save();
            }

            return new ConfigFile("songrequest.config");
        }

    }
}

