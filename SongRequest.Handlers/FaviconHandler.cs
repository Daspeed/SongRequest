using System.Net;

namespace SongRequest.Handlers
{
    public class FaviconHandler : BaseHandler
    {
        public override void Process(HttpListenerRequest request, HttpListenerResponse response)
        {
            using (var stream = typeof(StaticHandler).Assembly.GetManifestResourceStream("SongRequest.Static.favicon.ico"))
            {
                stream.CopyTo(response.OutputStream);
            }
        }
    }
}
