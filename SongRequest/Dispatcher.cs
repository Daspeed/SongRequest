using SongRequest.Handlers;
using SongRequest.Interfaces;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace SongRequest
{
    public class Dispatcher
    {
        private static Dictionary<string, IHandler> _mappings = new Dictionary<string, IHandler>{
            {string.Empty, new IndexHandler()},
            {"static", new StaticHandler()},
            {"dynamic", new DynamicHandler()},            
            {"favicon.ico", new FaviconHandler()},
            {"kill", new KillHandler()},
        };

        public static void ProcessRequest(HttpListenerContext context)
        {
            context.Response.ContentEncoding = Encoding.UTF8;

            string[] path = context.Request.RawUrl.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            _mappings[path.Length == 0 ? string.Empty : path[0]]
                .Process(context.Request, context.Response);

            context.Response.OutputStream.Flush();
            context.Response.OutputStream.Close();
        }
    }
}
