using System;

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
	}
}
