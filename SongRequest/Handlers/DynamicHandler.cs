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
            
			
			ISongplayer songPlayer = SongPlayerFactory.CreateSongPlayer();
			
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
                                long posted = long.Parse(reader.ReadToEnd());
                                Song song = songPlayer.GetPlayList(string.Empty, 0, 100).FirstOrDefault(x => x.TempId == posted);
                                if (song != null)
									songPlayer.Enqueue(song);
                            }
                            break;
                        case "DELETE":
                            using (var reader = new StreamReader(request.InputStream))
                            {
                                long posted = long.Parse(reader.ReadToEnd());
								//TODO: if the same song is in the list twice, which one is removed?
                                Song song = songPlayer.GetPlayList(string.Empty, 0, 100).FirstOrDefault(x => x.TempId == posted);
                                if (song != null)
									songPlayer.Dequeue(song);
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

                                response.ContentType = "application/json";
                                WriteUtf8String(response.OutputStream, JsonConvert.SerializeObject(
                                    songPlayer.GetPlayList(playlistRequest.Filter, playlistRequest.Page * _pageSize, _pageSize))
                                );
                            }
                        }
                        break;
                    }
                case "next":
                    response.ContentType = "application/json";
                    songPlayer.Next();
                    WriteUtf8String(response.OutputStream, JsonConvert.SerializeObject(songPlayer.PlayerStatus));
                    break;
                default:
                    response.ContentType = "text/plain";
                    WriteUtf8String(response.OutputStream, request.RawUrl);
                    break;
            }
        }
    }
}
