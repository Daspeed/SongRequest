using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SongRequest.Interfaces;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;

namespace SongRequest.Handlers
{
    public class ImageHandler : BaseHandler
    {
        public override void Process(HttpListenerRequest request, HttpListenerResponse response)
        {
            Match match = Regex.Match(request.RawUrl, "^/image/(.+)$");

            string resource = match.Groups[1].Value.Replace("/", ".");

            using (var stream = GetStream(resource))
            {
                stream.CopyTo(response.OutputStream);
            }
        }

        protected Stream GetStream(string name)
        {
            return typeof(StaticHandler).Assembly.GetManifestResourceStream("SongRequest.Static." + name);
        }
    }
}
