using System;
using System.Net;

namespace SongRequest.Handlers
{
    public class KillHandler : BaseHandler
    {
        public override void Process(HttpListenerRequest request, HttpListenerResponse response)
        {
            Program._running = false;
            Environment.Exit(0);
        }
    }
}
