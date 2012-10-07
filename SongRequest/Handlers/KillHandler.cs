using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SongRequest.Interfaces;
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
