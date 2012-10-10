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
    public class FaviconHandler : BaseHandler
    {
        public override void Process(HttpListenerRequest request, HttpListenerResponse response)
        {
            using (var stream = typeof(StaticHandler).Assembly.GetManifestResourceStream("SongRequest.Static.favicon.ico" ))
            {
                stream.CopyTo(response.OutputStream);
            }
        }
    }
}
