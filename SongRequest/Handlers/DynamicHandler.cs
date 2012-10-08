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
            string argument = actionPath.Length > 2 ? actionPath[2] : null;
			
			ISongplayer songPlayer = SongPlayerFactory.CreateSongPlayer();
			
            switch (action)
            {
                case "queue":
                    switch (request.HttpMethod)
                    {
                        case "GET":
                            response.ContentType = "application/json";
                            WriteUtf8String(response.OutputStream, JsonConvert.SerializeObject(songPlayer.PlayQueue.ToList()));
                            break;
                        case "POST":
                            using (var reader = new StreamReader(request.InputStream))
                            {
                                long posted = long.Parse(reader.ReadToEnd());
                                Song song = songPlayer.PlayList.FirstOrDefault(x => x.TempId == posted);
                                if (song != null)
									songPlayer.Enqueue(song);
                            }
                            break;
                        case "DELETE":
                            using (var reader = new StreamReader(request.InputStream))
                            {
                                long posted = long.Parse(reader.ReadToEnd());
								//TODO: if the same song is in the list twice, which one is removed?
                                Song song = songPlayer.PlayList.FirstOrDefault(x => x.TempId == posted);
                                if (song != null)
									songPlayer.Dequeue(song);
                            }
                            break;
                    }
                    break;
                case "playlist":
                    int page = int.Parse(argument);
                    response.ContentType = "application/json";
                    WriteUtf8String(response.OutputStream, JsonConvert.SerializeObject(
                        songPlayer.PlayList.Skip(page * _pageSize).Take(_pageSize))
                    );
                    break;
                case "playerstatus":
                    response.ContentType = "application/json";
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
