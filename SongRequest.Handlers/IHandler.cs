using System.Net;

namespace SongRequest.Interfaces
{
    public interface IHandler
    {
        void Process(HttpListenerRequest request, HttpListenerResponse response);
    }
}
