using SongRequest.Config;
using System;
using System.IO;
using System.Net;
using System.Text;

namespace SongRequest.Handlers
{
    public class IndexHandler : StaticHandler
    {
        public override void Process(HttpListenerRequest request, HttpListenerResponse response)
        {
            ConfigFile config = SongPlayerFactory.GetConfigFile();
            string text;

            string ui = config.GetValue("server.ui");
            if (ui.Equals("mobile", StringComparison.OrdinalIgnoreCase))
                text = Get("index_mobile.htm");
            else
                text = Get("index.htm");

            response.ContentType = "text/html";

            WriteUtf8String(response.OutputStream, text);
        }
    }
}
