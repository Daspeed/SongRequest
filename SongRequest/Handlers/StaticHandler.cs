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
    public class StaticHandler : BaseHandler
    {
        public override void Process(HttpListenerRequest request, HttpListenerResponse response)
        {
            Match match = Regex.Match(request.RawUrl, "^/static/(.+)$");

            string resource = match.Groups[1].Value.Replace("/", ".");

            if (resource.EndsWith("png", StringComparison.OrdinalIgnoreCase))
            {
                using (var stream = GetStream(resource))
                {
                    stream.CopyTo(response.OutputStream);
                }
                return;
            }
            else
            {
                string text = Get(resource);

                response.ContentType = "text/html";

                WriteUtf8String(response.OutputStream, text);
            }
        }

        protected string Get(string name)
        {
            using (var stream = GetStream(name))
            {
                if (stream == null)
                {
                    Console.Error.WriteLine("Could not find static content: {0}", name);
                    return null;
                }
                using (var streamReader = new StreamReader(stream))
                    return streamReader.ReadToEnd();
            }
        }

        protected Stream GetStream(string name)
        {
            return typeof(StaticHandler).Assembly.GetManifestResourceStream("SongRequest.Static." + name);
        }
    }
}
