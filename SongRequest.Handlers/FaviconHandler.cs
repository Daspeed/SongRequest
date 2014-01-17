using System;
using System.IO;
using System.Net;
using System.Reflection;

namespace SongRequest.Handlers
{
    public class FaviconHandler : BaseHandler
    {
        public override void Process(HttpListenerRequest request, HttpListenerResponse response)
        {
            using (var stream = Assembly.GetEntryAssembly().GetManifestResourceStream("SongRequest.Static.favicon.ico"))
            {
                response.ContentType = "image/x-icon";
                stream.CopyTo(response.OutputStream);
            }
        }
    }
}
