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
            string text = GetIndex("index.htm");

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
