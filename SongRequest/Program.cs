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
            using (HttpListener listener = new HttpListener())
            {
                int port = ConfigReader.SongRequestPort ?? 8765;

                listener.Prefixes.Add(string.Format("http://*:{0}/", port));
                listener.Start();
                Console.WriteLine("Listening on port: {0}...", port);
                while (_running)
                {
                    HttpListenerContext context = listener.GetContext();
                    Stopwatch watch = Stopwatch.StartNew();

                    Console.WriteLine("Accepted new request - {0}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                    Console.WriteLine("{0}\t{1}", context.Request.UserHostAddress, context.Request.RawUrl);

                    try
                    {
                        Dispatcher.ProcessRequest(context);
                        Console.WriteLine("Request ended - {0}ms", watch.ElapsedMilliseconds);
						Console.WriteLine("Currently playing: {0}", SongPlayerFactory.CreateSongPlayer().PlayerStatus.Song.FileName);
						Console.WriteLine("Position: {0}", SongPlayerFactory.CreateSongPlayer().PlayerStatus.Position);
                        watch.Stop();
                    }
                    catch (Exception ex)
                    {
                        context.Response.StatusCode = 500;

                        using (var writer = new StreamWriter(context.Response.OutputStream))
                            writer.Write(ex.ToString());
                    }
                    Console.WriteLine();
                }
            }

            Console.WriteLine("No longer listening");
        }
    }
}


//using (Process p = new Process())
//            {
//                ProcessStartInfo info = new ProcessStartInfo(@"C:\Program Files (x86)\VideoLAN\VLC\vlc.exe", "--intf http --http-host 127.0.0.1:8090");
//                info.CreateNoWindow = true;
//                info.UseShellExecute = true;
//                info.WindowStyle = ProcessWindowStyle.Normal;
//                p.StartInfo = info;
//                p.Start();
//            }