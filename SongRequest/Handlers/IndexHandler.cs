using System.Net;

namespace SongRequest.Handlers
{
    public class IndexHandler : StaticHandler
    {
        public override void Process(HttpListenerRequest request, HttpListenerResponse response)
        {
            string text = Get("index.htm");

            response.ContentType = "text/html";

            WriteUtf8String(response.OutputStream, text);
        }
    }
}
