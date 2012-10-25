using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;

namespace SongRequest.Handlers
{
    public class PageNotFoundHandler : StaticHandler
    {
        public override void Process(HttpListenerRequest request, HttpListenerResponse response)
        {
            string text = Get("404.htm");

            response.ContentType = "text/html";

            response.StatusCode = 404;
            WriteUtf8String(response.OutputStream, text ?? string.Empty);
        }
    }
}
