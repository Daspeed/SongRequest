using System.Net;

namespace SongRequest.Handlers
{
    public interface IHandler
    {
        void Process(HttpListenerRequest request, HttpListenerResponse response);
    }
}
