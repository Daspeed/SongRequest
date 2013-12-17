using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using SongRequest.SongPlayer;
using SongRequest.Utils;

namespace SongRequest.Handlers
{
    public class DynamicHandler : BaseHandler
    {
        const int _pageSize = 50;

        public bool ClientAllowed(string requesterName)
        {
            if (string.IsNullOrEmpty(requesterName))
                return true;

            string allowedClients = SongPlayerFactory.GetConfigFile().GetValue("server.clients");

            //Only allow clients from config file
            return string.IsNullOrEmpty(allowedClients) ||
                    allowedClients.Equals("all", StringComparison.OrdinalIgnoreCase) ||
                    SongPlayerFactory.GetConfigFile().GetValue("server.clients").ContainsOrdinalIgnoreCase(requesterName);
        }

        public override void Process(HttpListenerRequest request, HttpListenerResponse response)
        {
            string[] actionPath = request.RawUrl.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            string action = actionPath[1];

            ISongplayer songPlayer = SongPlayerFactory.GetSongPlayer();
            string requester = GetRequester(request);
            if (!ClientAllowed(requester))
                return;

            response.AppendHeader("Cache-Control", "no-cache");

            switch (action)
            {
                case "queue":
                    switch (request.HttpMethod)
                    {
                        case "GET":
                            response.ContentType = "application/json";
                            WriteUtf8String(response.OutputStream, JsonConvert.SerializeObject(
                                new
                                {
                                    Queue = songPlayer.PlayQueue.ToList(),
                                    PlayerStatus = songPlayer.PlayerStatus,
                                    Self = requester
                                }
                            ));
                            break;
                        case "POST":
                            using (var reader = new StreamReader(request.InputStream))
                            {
                                songPlayer.Enqueue(reader.ReadToEnd(), requester);
                            }
                            break;
                        case "DELETE":
                            using (var reader = new StreamReader(request.InputStream))
                            {
                                songPlayer.Dequeue(reader.ReadToEnd(), requester);
                            }
                            break;
                    }
                    break;
                case "playlist":
                    {
                        if (request.HttpMethod == "POST")
                        {
                            using (var reader = new StreamReader(request.InputStream))
                            {
                                string posted = reader.ReadToEnd();
                                var playlistRequest = JsonConvert.DeserializeAnonymousType(posted, new { Filter = string.Empty, Page = 0, SortBy = "artist", Ascending = true });

                                Song[] songs = songPlayer.GetPlayList(
                                    playlistRequest.Filter,
                                    playlistRequest.SortBy,
                                    playlistRequest.Ascending
                                ).ToArray();

                                response.ContentType = "application/json";
                                WriteUtf8String(response.OutputStream, JsonConvert.SerializeObject(
                                    new
                                    {
                                        TotalPageCount = (songs.Length + (_pageSize - 1)) / _pageSize,
                                        CurrentPage = playlistRequest.Page,
                                        SongsForCurrentPage = songs.Skip((playlistRequest.Page - 1) * _pageSize).Take(_pageSize).ToArray(),
                                        SortBy = playlistRequest.SortBy,
                                        Ascending = playlistRequest.Ascending
                                    }
                                ));
                            }
                        }
                        break;
                    }
                case "next":
                    response.ContentType = "application/json";
                    songPlayer.Next(requester);
                    break;
                case "rescan":
                    response.ContentType = "application/json";
                    songPlayer.Rescan();
                    break;
                case "pause":
                    response.ContentType = "application/json";
                    songPlayer.Pause();
                    break;
                case "volume":
                    response.ContentType = "application/json";
                    if (request.HttpMethod == "POST")
                    {
                        using (var reader = new StreamReader(request.InputStream))
                        {
                            string posted = reader.ReadToEnd();

                            int volume;
                            if (int.TryParse(posted, out volume))
                            {
                                songPlayer.Volume = volume;
                            }

                            WriteUtf8String(response.OutputStream, JsonConvert.SerializeObject(songPlayer.Volume));
                        }
                    }
                    break;
                default:
                    response.ContentType = "text/plain";
                    WriteUtf8String(response.OutputStream, request.RawUrl);
                    break;
            }
        }

        private static object _lockObject = new object();
        private static Dictionary<string, string> ipToHostName = new Dictionary<string, string>();

        /// <summary>
        /// Get requester
        /// </summary>
        /// <param name="request">Request</param>
        /// <returns>Requester as string</returns>
        private static string GetRequester(HttpListenerRequest request)
        {
            if (request != null && request.RemoteEndPoint != null && request.RemoteEndPoint.Address != null)
            {
                string hostName;

                lock (_lockObject)
                {
                    string ip = request.RemoteEndPoint.Address.ToString();

                    if (ipToHostName.ContainsKey(ip))
                        return ipToHostName[ip];

                    try
                    {
                        hostName = Dns.GetHostEntry(request.RemoteEndPoint.Address).HostName;
                    }
                    catch (Exception)
                    {
                        hostName = request.RemoteEndPoint.Address.ToString();
                    }

                    ipToHostName.Add(ip, hostName);
                }

                return hostName;
            }

            return "unknown";
        }
    }
}
