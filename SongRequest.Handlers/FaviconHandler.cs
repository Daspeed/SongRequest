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
            string fullPath = Path.Combine(Environment.CurrentDirectory, "SongRequest.exe");
            using (var stream = Assembly.LoadFile(fullPath).GetManifestResourceStream("SongRequest.Static.favicon.ico"))
            {
                stream.CopyTo(response.OutputStream);
            }
        }
    }
}
