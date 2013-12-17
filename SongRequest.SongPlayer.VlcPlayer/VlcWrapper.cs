using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace SongRequest
{
    public class VlcWrapper
    {
        [DllImport(@"C:\Program Files (x86)\VideoLAN\VLC\libvlc", EntryPoint = "libvlc_new", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr NewCore(int argc, IntPtr argv);

        [DllImport(@"C:\Program Files (x86)\VideoLAN\VLC\libvlc", EntryPoint = "libvlc_media_player_new", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr NewPlayer(IntPtr instance);

        [DllImport(@"C:\Program Files (x86)\VideoLAN\VLC\libvlc", EntryPoint = "libvlc_media_new_path", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr NewMedia(IntPtr instance, string path);

        [DllImport(@"C:\Program Files (x86)\VideoLAN\VLC\libvlc", EntryPoint = "libvlc_media_player_set_media", CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetMedia(IntPtr player, IntPtr media);

        [DllImport(@"C:\Program Files (x86)\VideoLAN\VLC\libvlc", EntryPoint = "libvlc_audio_set_volume", CallingConvention = CallingConvention.Cdecl)]
        public static extern int SetVolume(IntPtr player, int volume);

        [DllImport(@"C:\Program Files (x86)\VideoLAN\VLC\libvlc", EntryPoint = "libvlc_audio_get_volume", CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetVolume(IntPtr player);

        [DllImport(@"C:\Program Files (x86)\VideoLAN\VLC\libvlc", EntryPoint = "libvlc_media_player_get_time", CallingConvention = CallingConvention.Cdecl)]
        public static extern long GetPosition(IntPtr player);

        // IDLE/CLOSE=0, OPENING=1, BUFFERING=2, PLAYING=3, PAUSED=4, STOPPING=5, ENDED=6, ERROR=7
        [DllImport(@"C:\Program Files (x86)\VideoLAN\VLC\libvlc", EntryPoint = "libvlc_media_player_get_state", CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetState(IntPtr player);

        [DllImport(@"C:\Program Files (x86)\VideoLAN\VLC\libvlc", EntryPoint = "libvlc_media_player_play", CallingConvention = CallingConvention.Cdecl)]
        public static extern int Play(IntPtr player);

        [DllImport(@"C:\Program Files (x86)\VideoLAN\VLC\libvlc", EntryPoint = "libvlc_media_player_pause", CallingConvention = CallingConvention.Cdecl)]
        public static extern void Pause(IntPtr player);

        [DllImport(@"C:\Program Files (x86)\VideoLAN\VLC\libvlc", EntryPoint = "libvlc_media_player_pause", CallingConvention = CallingConvention.Cdecl)]
        public static extern void Stop(IntPtr player);



        private IntPtr instance;
        private IntPtr player;

        public bool Playing
        {
            get
            {
                int state = GetState(player);
                return (state == 1 || state == 2 || state == 3 || state == 4);
            }
        }
        public VlcWrapper()
        {
            this.instance = VlcWrapper.NewCore(0, IntPtr.Zero);
            player = NewPlayer(instance);
        }

        public void Pause()
        {
            Pause(player);
        }

        public void Stop()
        {
            Stop(player);
        }

        public long Position
        {
            get
            {
                return GetPosition(player);
            }
        }

        public void PlaySong(string name)
        {
            IntPtr media = NewMedia(instance, name);
            SetMedia(player, media);
            Play(player);
        }

        public int Volume
        {
            get
            {
                return GetVolume(player);
            }
            set
            {
                HashSet<int> invalidStates = new HashSet<int> { 0, 1, 2, 5, 6, 7 };
                int state = GetState(player);
                if (invalidStates.Contains(state)) // not ready at this time
                {
                    new Thread(x => {
                        do
                        {
                            state = GetState(player);
                            Thread.Sleep(10);
                        }
                        while (invalidStates.Contains(state));
                        SetVolume(player, value);
                    }).Start();
                }
                else
                {
                    SetVolume(player, value);
                }
            }
        }
    }
}
