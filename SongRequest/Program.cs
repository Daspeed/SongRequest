using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading;
using SongRequest.Interfaces;
using SongRequest.Handlers;
using System.Security.Permissions;
using System.IO;
using System.Diagnostics;

namespace SongRequest
{
    class Program
    {
        internal volatile static bool _running = true;
        
        static void Main(string[] args)
        {
            try
            {
                Run();
            }
            catch (Exception ex)
            {
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
                int port;

                if (!int.TryParse(SongPlayerFactory.GetConfigFile().GetValue("server.port"), out port))
                    port = 8765;

                Console.SetCursorPosition(0, 1);
                Console.Write("Listening on port: {0}", port);
                Console.SetCursorPosition(0, 2);
                Console.Write("Library: {0}", SongPlayerFactory.GetConfigFile().GetValue("library.path"));

                SongPlayerFactory.GetSongPlayer().LibraryStatusChanged += new StatusChangedEventHandler(Program_LibraryStatusChanged);
                SongPlayerFactory.GetSongPlayer().PlayerStatusChanged += new StatusChangedEventHandler(Program_PlayerStatusChanged);

                listener.Prefixes.Add(string.Format("http://*:{0}/", port));
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

        static void DrawArt()
        {
            lock (consoleLock)
            {
                string white = new string(' ', Console.WindowWidth - 36);
                Console.SetCursorPosition(0, Console.WindowHeight-10);
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
