using Microsoft.Win32;
using SongRequest.Config;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace SongRequest.SongPlayer.VlcPlayer
{
    [Export(typeof(IMediaDevice))]
    public class VlcWrapper : IMediaDevice
    {
        [DllImport(@"libvlc", EntryPoint = "libvlc_new", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr NewCore(int argc, IntPtr argv);

        [DllImport(@"libvlc", EntryPoint = "libvlc_media_player_new", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr NewPlayer(IntPtr instance);

        [DllImport(@"libvlc", EntryPoint = "libvlc_media_new_path", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr NewMedia(IntPtr instance, [MarshalAs(UnmanagedType.LPArray)] byte[] path);

        [DllImport(@"libvlc", EntryPoint = "libvlc_media_player_set_media", CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetMedia(IntPtr player, IntPtr media);

        [DllImport(@"libvlc", EntryPoint = "libvlc_audio_set_volume", CallingConvention = CallingConvention.Cdecl)]
        public static extern int SetVolume(IntPtr player, int volume);

        [DllImport(@"libvlc", EntryPoint = "libvlc_audio_get_volume", CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetVolume(IntPtr player);

        [DllImport(@"libvlc", EntryPoint = "libvlc_media_player_get_time", CallingConvention = CallingConvention.Cdecl)]
        public static extern long GetPosition(IntPtr player);

        // IDLE/CLOSE=0, OPENING=1, BUFFERING=2, PLAYING=3, PAUSED=4, STOPPING=5, ENDED=6, ERROR=7
        [DllImport(@"libvlc", EntryPoint = "libvlc_media_player_get_state", CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetState(IntPtr player);

        [DllImport(@"libvlc", EntryPoint = "libvlc_media_player_play", CallingConvention = CallingConvention.Cdecl)]
        public static extern int Play(IntPtr player);

        [DllImport(@"libvlc", EntryPoint = "libvlc_media_player_pause", CallingConvention = CallingConvention.Cdecl)]
        public static extern void Pause(IntPtr player);

        [DllImport(@"libvlc", EntryPoint = "libvlc_media_player_pause", CallingConvention = CallingConvention.Cdecl)]
        public static extern void Stop(IntPtr player);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetDllDirectory(string lpPathName);
        
        private IntPtr _instance;
        private IntPtr _player;

        public VlcWrapper()
        {
            // do runtime check for windows
            if (Settings.IsRunningOnWindows())
                InitializeWindowsPath();

            this._instance = VlcWrapper.NewCore(0, IntPtr.Zero);
            _player = NewPlayer(_instance);
        }

        public bool IsPlaying
        {
            get
            {
                int state = GetState(_player);
                return (state == 1 || state == 2 || state == 3 || state == 4);
            }
        }

        public void InitializeWindowsPath()
        {
            string vlcPath = GetInstallDirFromRegistry() ?? GetProgramFilesPath();

            if (Directory.Exists(vlcPath))
            {
                // set vlc path as search directory for loadlibrary function
                SetDllDirectory(vlcPath);
            }
        }

        public string GetInstallDirFromRegistry()
        {
            return Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\VideoLAN\VLC", "InstallDir", string.Empty) as string;
        }

        public string GetProgramFilesPath()
        {
            //VLC gets installed in program files
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"VideoLAN\VLC\");
        }

        public void Pause()
        {
            Pause(_player);
        }

        public void Stop()
        {
            Stop(_player);
        }

        public long Position
        {
            get
            {
                return GetPosition(_player);
            }
        }

        public void PlaySong(string name)
        {
            IntPtr media = NewMedia(_instance, Encoding.UTF8.GetBytes(name));
            SetMedia(_player, media);
            Play(_player);
        }

        public int Volume
        {
            get
            {
                return GetVolume(_player);
            }
            set
            {
                HashSet<int> invalidStates = new HashSet<int> { 0, 1, 2, 5, 6, 7 };
                int state = GetState(_player);
                if (invalidStates.Contains(state)) // not ready at this time
                {
                    new Thread(x => {
                        do
                        {
                            state = GetState(_player);
                            Thread.Sleep(10);
                        }
                        while (invalidStates.Contains(state));
                        SetVolume(_player, value);
                    }).Start();
                }
                else
                {
                    SetVolume(_player, value);
                }
            }
        }
    }
}
