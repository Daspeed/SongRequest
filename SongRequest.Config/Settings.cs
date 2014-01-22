using System;

namespace SongRequest.Config
{
    public class Settings
    {
        public static bool IsMonoRuntime()
        {
            return Type.GetType("Mono.Runtime") != null;
        }

        public static bool IsRunningOnWindows()
        {
            return Environment.OSVersion.Platform == PlatformID.Win32NT ||
                    Environment.OSVersion.Platform == PlatformID.Win32S ||
                    Environment.OSVersion.Platform == PlatformID.Win32Windows ||
                    Environment.OSVersion.Platform == PlatformID.WinCE ||
                    Environment.OSVersion.Platform == PlatformID.Xbox;
        }

        public static bool IsRunningOnUnix()
        {
            return Environment.OSVersion.Platform == PlatformID.MacOSX ||
            Environment.OSVersion.Platform == PlatformID.Unix;
        }
    }
}

