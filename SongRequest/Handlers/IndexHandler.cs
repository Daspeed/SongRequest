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
                text = GetIndex("index_mobile.htm");
            else
                text = GetIndex("index.htm");

            response.ContentType = "text/html";

            WriteUtf8String(response.OutputStream, text);
        }

        public string GetIndex(string name)
        {
#if DEBUG
            string content = File.ReadAllText(Path.GetFullPath(Environment.CurrentDirectory + @"\..\..\Static\" + name), Encoding.UTF8);
            return content;
#else
            return Get(name);
#endif
        }
    }
}
