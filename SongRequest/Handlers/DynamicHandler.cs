using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SongRequest.Interfaces;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace SongRequest.Handlers
{
    public class DynamicHandler : BaseHandler
    {
        const int _pageSize = 50;

        public override void Process(HttpListenerRequest request, HttpListenerResponse response)
        {
            string[] actionPath = request.RawUrl.Split(new[]{'/'}, StringSplitOptions.RemoveEmptyEntries);

            string action = actionPath[1];
            
			ISongplayer songPlayer = SongPlayerFactory.GetSongPlayer();

            switch (action)
            {
                case "queue":
                    switch (request.HttpMethod)
                    {
                        case "GET":
                            response.ContentType = "application/json";
                            WriteUtf8String(response.OutputStream, JsonConvert.SerializeObject(
                                new {
                                    Queue = songPlayer.PlayQueue.ToList(),
                                    PlayerStatus = songPlayer.PlayerStatus
                                }
                            ));
                            break;
                        case "POST":
                            using (var reader = new StreamReader(request.InputStream))
                            {
                                songPlayer.Enqueue(reader.ReadToEnd(), GetRequester(request));
                            }
                            break;
                        case "DELETE":
                            using (var reader = new StreamReader(request.InputStream))
                            {
                                songPlayer.Dequeue(reader.ReadToEnd(), GetRequester(request));
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
                                var playlistRequest = JsonConvert.DeserializeAnonymousType(posted, new { Filter = string.Empty, Page = 0 });

                                Song[] songs = songPlayer.GetPlayList(playlistRequest.Filter, 0, int.MaxValue).ToArray();

                                response.ContentType = "application/json";
                                WriteUtf8String(response.OutputStream, JsonConvert.SerializeObject(
                                    new{
                                        TotalPageCount = (songs.Length + (_pageSize-1)) / _pageSize,
                                        CurrentPage = playlistRequest.Page,
                                        SongsForCurrentPage = songs.Skip((playlistRequest.Page-1) * _pageSize).Take(_pageSize).ToArray()
                                    }
                                ));
                            }
                        }
                        break;
                    }
                case "next":
                    response.ContentType = "application/json";
                    songPlayer.Next(GetRequester(request));
                    WriteUtf8String(response.OutputStream, JsonConvert.SerializeObject(songPlayer.PlayerStatus));
                    break;
                case "volume":
                    response.ContentType = "application/json";
                    if (request.HttpMethod == "POST")
                    {
                        using (var reader = new StreamReader(request.InputStream))
                        {
                            string posted = reader.ReadToEnd();
                            songPlayer.Volume = int.Parse(posted);

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

        private static string GetRequester(HttpListenerRequest request)
        {
            string requester;

            if (request.RemoteEndPoint != null)
                requester = Dns.GetHostEntry(request.RemoteEndPoint.Address).HostName;
            else
                requester = "unknown";
            return requester;
        }
    }
}
