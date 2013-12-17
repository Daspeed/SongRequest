using SongRequest.Handlers;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace SongRequest
{
    public class Dispatcher
    {
        private static Dictionary<string, IHandler> _mappings = new Dictionary<string, IHandler>
        {
            {string.Empty, new IndexHandler(typeof(Dispatcher).Assembly)},
            {"static", new StaticHandler(typeof(Dispatcher).Assembly)},
            {"dynamic", new DynamicHandler()},            
            {"favicon.ico", new FaviconHandler()}
        };

        public static void ProcessRequest(HttpListenerContext context)
        {
            context.Response.ContentEncoding = Encoding.UTF8;

            string[] path = context.Request.RawUrl.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            string searchKey = path.Length == 0 ? string.Empty : path[0];

            if (_mappings.ContainsKey(searchKey))
                _mappings[searchKey].Process(context.Request, context.Response);

            context.Response.OutputStream.Flush();
            context.Response.OutputStream.Close();
        }
    }
}
