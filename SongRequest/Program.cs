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

            SongPlayerFactory.CreateSongPlayer().LibraryStatusChanged += new StatusChangedEventHandler(Program_LibraryStatusChanged);
            SongPlayerFactory.CreateSongPlayer().PlayerStatusChanged += new StatusChangedEventHandler(Program_PlayerStatusChanged);

            using (HttpListener listener = new HttpListener())
            {
                int port = ConfigReader.SongRequestPort ?? 8765;

                listener.Prefixes.Add(string.Format("http://*:{0}/", port));
                listener.Start();
                Console.SetCursorPosition(0, 1);
                Console.Write("Listening on port: {0}...", port);
                while (_running)
                {
                    HttpListenerContext context = listener.GetContext();
                    Stopwatch watch = Stopwatch.StartNew();

                    //Console.WriteLine("Accepted new request - {0}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                    //Console.WriteLine("{0}\t{1}", context.Request.UserHostAddress, context.Request.RawUrl);
                    Program_LastRequestChanged(string.Format("{0} - {1}\t{2}", DateTime.Now.ToString("HH:mm:ss"), context.Request.UserHostAddress, context.Request.RawUrl));

                    try
                    {
                        Dispatcher.ProcessRequest(context);
                        //Console.WriteLine("Request ended - {0}ms", watch.ElapsedMilliseconds);

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

            //Console.WriteLine("No longer listening");
        }

        static void Program_LastRequestChanged(string status)
        {
            Console.SetCursorPosition(0, 5);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, 5);
            Console.Write(status.Substring(0, Math.Min(status.Length, Console.WindowWidth)));
        }

        static void Program_PlayerStatusChanged(string status)
        {
            Console.SetCursorPosition(0, 2);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, 2);
            Console.Write(status.Substring(0, Math.Min(status.Length, Console.WindowWidth)));
        }

        static void Program_LibraryStatusChanged(string status)
        {
            Console.SetCursorPosition(0, 3);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, 3);
            Console.Write(status.Substring(0, Math.Min(status.Length, Console.WindowWidth)));
        }
    }
}
