using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using SongRequest.Config;

namespace SongRequest.SongPlayer
{
    public class SongPlayerFactory
    {
        private static object _lockObject = new object();
        private static SongPlayerFactory _factory;

        public static SongPlayerFactory GetFactory()
        {
            lock (_lockObject)
            {
                if (_factory == null)
                    _factory = new SongPlayerFactory();
            }

            return _factory;
        }

        public static ISongplayer GetSongPlayer()
        {
            return GetFactory().SongPlayer;
        }

        [Import(typeof(ISongplayer))]
        public ISongplayer SongPlayer { get; set; }
        
        private SongPlayerFactory()
        {
            InitializeAddinCatalog();
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

        public void InitializeAddinCatalog()
        {
            DirectoryCatalog catalog = new DirectoryCatalog("addins");
            CompositionContainer container = new CompositionContainer(catalog);
            try
            {
                container.ComposeParts(this);
            }
            catch (CompositionException ex)
            {
                throw ex;
            }

        }

    }
}

