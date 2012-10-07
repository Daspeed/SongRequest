using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SongRequest.Interfaces;
using System.Net;
using System.IO;

namespace SongRequest.Handlers
{
    public abstract class BaseHandler : IHandler
    {
        public abstract void Process(HttpListenerRequest request, HttpListenerResponse response);

        public virtual void WriteUtf8String(Stream stream, string text)
        {
            using (var writer = new StreamWriter(stream, Encoding.UTF8))
                writer.Write(text);
        }
    }
}
