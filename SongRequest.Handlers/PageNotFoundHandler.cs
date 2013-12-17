using System.Net;

namespace SongRequest.Handlers
{
    public class PageNotFoundHandler : StaticHandler
    {
		public PageNotFoundHandler (System.Reflection.Assembly resourceAssembly) :
			base(resourceAssembly)
		{
			
		}

        public override void Process(HttpListenerRequest request, HttpListenerResponse response)
        {
            string text = Get("404.htm");

            response.ContentType = "text/html";

            response.StatusCode = 404;
            WriteUtf8String(response.OutputStream, text ?? string.Empty);
        }
    }
}
