using System.Net;
using SongRequest.Config;
using SongRequest.SongPlayer;
using System;
using System.IO;

namespace SongRequest.Handlers
{
    public class IndexHandler : StaticHandler
    {
        public IndexHandler(Func<string, Stream> resourceGetter) :
            base(resourceGetter)
		{
			
		}

        public override void Process(HttpListenerRequest request, HttpListenerResponse response)
        {
            SongPlayerFactory.GetConfigFile();

            string text = Get("index.htm");
            response.ContentType = "text/html";

            WriteUtf8String(response.OutputStream, text);
        }
    }
}
