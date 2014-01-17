using System;

namespace SongRequest.SongPlayer
{
    public interface IMediaDevice
    {
        void Stop();
        void Pause();
        void PlaySong(string name);

        bool IsPlaying { get; }
        long Position { get; }
        int Volume { get; set; }
    }
}
