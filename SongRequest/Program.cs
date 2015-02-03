using SongRequest.SongPlayer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace SongRequest
{
    class Program
    {
        internal volatile static bool _running = true;

        private static string host;
        private static int port;

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
                    Console.WriteLine("  netsh http add urlacl url={0} user={1}\\{2} listen=yes", Prefix, userdomain, username);
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

        private static string Prefix
        {
            get
            {
                return string.Format("http://{0}:{1}/", host, port);
            }
        }

        private async static void Run()
        {
            Console.Clear();
            DrawArt();

            using (HttpListener listener = new HttpListener())
            {
                host = SongPlayerFactory.GetConfigFile().GetValue("server.host");
                if (string.IsNullOrWhiteSpace(host))
                    host = "*";

                if (!int.TryParse(SongPlayerFactory.GetConfigFile().GetValue("server.port"), out port))
                    port = 8765;

                DrawProgramStatus();

                string stringVolume = SongPlayerFactory.GetConfigFile().GetValue("player.startupvolume");
                int volume;
                if (!int.TryParse(stringVolume, out volume))
                    volume = 50;

                SongPlayerFactory.GetSongPlayer().Volume = volume;
                SongPlayerFactory.GetSongPlayer().LibraryStatusChanged += new StatusChangedEventHandler(Program_LibraryStatusChanged);
                SongPlayerFactory.GetSongPlayer().PlayerStatusChanged += new StatusChangedEventHandler(Program_PlayerStatusChanged);

                listener.Prefixes.Add(Prefix);

                listener.Start();

                // maximum number of tasks
                int maximumNumberOfTasks = 4 * Environment.ProcessorCount;
                Program_LastRequestChanged(string.Format("Asynchronous handles a maximum of {0} requests.", maximumNumberOfTasks));

                // list of tasks
                List<Task> tasks = new List<Task>();

                using (SemaphoreSlim semaphore = new SemaphoreSlim(maximumNumberOfTasks, maximumNumberOfTasks))
                {
                    while (_running)
                    {
                        // wait until a semaphore request is released
                        await semaphore.WaitAsync();

                        tasks.Add(Task.Run(() =>
                        {
                            listener.GetContextAsync().ContinueWith(async (t) =>
                            {
                                HttpListenerContext context = await t;

                                try
                                {
                                    Program_LastRequestChanged(string.Format("{0} - {1}\t{2}", DateTime.Now.ToString("HH:mm:ss"), context.Request.UserHostAddress, context.Request.RawUrl));

                                    Dispatcher.ProcessRequest(context);
                                    return;
                                }
                                catch (Exception ex)
                                {
                                    context.Response.StatusCode = 500;

                                    using (var writer = new StreamWriter(context.Response.OutputStream))
                                        writer.Write(ex.ToString());
                                }
                                finally
                                {
                                    // release semaphore request
                                    semaphore.Release();
                                }
                            });
                        }));
                    }
                }

                await Task.WhenAll(tasks);
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
                Console.Write("Listening on: {0}", Prefix);
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
