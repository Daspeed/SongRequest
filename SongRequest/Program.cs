using System;
using System.Linq;
using System.Diagnostics;
using System.IO;
using System.Net;
using SongRequest.SongPlayer;

namespace SongRequest
{
    class Program
    {
        internal volatile static bool _running = true;
        private static int port;

        static string prefix = "http://*:8765/";


        static void Main(string[] args)
        {
            try
            {
                Run();
            }
            catch (HttpListenerException ex)
            {
                if (ex.ErrorCode == 5)
                {
                    string username = Environment.GetEnvironmentVariable("USERNAME");
                    string userdomain = Environment.GetEnvironmentVariable("USERDOMAIN");

                    Console.SetCursorPosition(0, Console.WindowHeight - 10);
                    Console.WriteLine("You need to run the following command (as admin):");
                    Console.WriteLine("  netsh http add urlacl url={0} user={1}\\{2} listen=yes", prefix, userdomain, username);
                    Console.SetCursorPosition(0, 0);
                }
                else
                {
                    Console.WriteLine();
                    Console.WriteLine();
                    Console.WriteLine(ex);
                    Console.ReadLine();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine(ex);
                Console.ReadLine();
            }
        }

        private static void Run()
        {
            Console.Clear();
            DrawArt();
            using (HttpListener listener = new HttpListener())
            {
                if (!int.TryParse(SongPlayerFactory.GetConfigFile().GetValue("server.port"), out port))
                    port = 8765;

                prefix = string.Format("http://*:{0}/", port);

                DrawProgramStatus();

                string stringVolume = SongPlayerFactory.GetConfigFile().GetValue("player.startupvolume");
                int volume;
                if (!int.TryParse(stringVolume, out volume))
                    volume = 50;

                SongPlayerFactory.GetSongPlayer().Volume = volume;
                SongPlayerFactory.GetSongPlayer().LibraryStatusChanged += new StatusChangedEventHandler(Program_LibraryStatusChanged);
                SongPlayerFactory.GetSongPlayer().PlayerStatusChanged += new StatusChangedEventHandler(Program_PlayerStatusChanged);

                listener.Prefixes.Add(prefix);

                listener.Start();

                while (_running)
                {
                    HttpListenerContext context = listener.GetContext();
                    Stopwatch watch = Stopwatch.StartNew();

                    Program_LastRequestChanged(string.Format("{0} - {1}\t{2}", DateTime.Now.ToString("HH:mm:ss"), context.Request.UserHostAddress, context.Request.RawUrl));

                    try
                    {
                        Dispatcher.ProcessRequest(context);

                        watch.Stop();
                    }
                    catch (Exception ex)
                    {
                        context.Response.StatusCode = 500;

                        using (var writer = new StreamWriter(context.Response.OutputStream))
                            writer.Write(ex.ToString());
                    }
                }
            }
        }

        static object consoleLock = new object();

        static void Program_LibraryStatusChanged(string status)
        {
            lock (consoleLock)
            {
                string[] directories = SongPlayerFactory.GetConfigFile().GetValue("library.path").Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

                string libraryPath = string.Join("';'", directories.Select(x => Directory.Exists(x) ? x : x + "- please create folder"));

                Console.SetCursorPosition(0, 2);
                Console.Write(new string(' ', Console.WindowWidth));
                Console.SetCursorPosition(0, 2);
                Console.Write("Library: '{0}'", libraryPath);
                Console.SetCursorPosition(0, 3);
                Console.Write(new string(' ', Console.WindowWidth));
                Console.SetCursorPosition(0, 3);
                Console.Write(status.Substring(0, Math.Min(status.Length, Console.WindowWidth)));
            }
        }

        static void Program_PlayerStatusChanged(string status)
        {
            lock (consoleLock)
            {
                Console.SetCursorPosition(0, 4);
                Console.Write(new string(' ', Console.WindowWidth));
                Console.SetCursorPosition(0, 4);
                Console.Write(status.Substring(0, Math.Min(status.Length, Console.WindowWidth)));
            }
        }

        static void Program_LastRequestChanged(string status)
        {
            lock (consoleLock)
            {
                Console.SetCursorPosition(0, 6);
                Console.Write(new string(' ', Console.WindowWidth));
                Console.SetCursorPosition(0, 6);
                Console.Write(status.Substring(0, Math.Min(status.Length, Console.WindowWidth)));
            }
        }

        static void DrawProgramStatus()
        {
            lock (consoleLock)
            {
                Console.SetCursorPosition(0, 1);
                Console.Write("Listening on port: {0}", port);
            }
        }

        static void DrawArt()
        {
            lock (consoleLock)
            {
                int width = Console.WindowWidth == 0 ? 0 : Console.WindowWidth - 36;
                int height = Console.WindowHeight == 0 ? 0 : Console.WindowHeight - 10;

                string white = new string(' ', width);
                Console.SetCursorPosition(0, height);
                Console.WriteLine(white + @"    ,");
                Console.WriteLine(white + @"    |\        __");
                Console.WriteLine(white + @"    | |      |--|             __");
                Console.WriteLine(white + @"    |/       |  |            |~'");
                Console.WriteLine(white + @"   /|_      () ()            |");
                Console.WriteLine(white + @"  //| \             |\      ()");
                Console.WriteLine(white + @" | \|_ |            | \ ");
                Console.WriteLine(white + @"  \_|_/            ()  |");
                Console.WriteLine(white + @"    |                  |");
                Console.WriteLine(white + @"   @'                 ()");
            }
        }
    }
}
