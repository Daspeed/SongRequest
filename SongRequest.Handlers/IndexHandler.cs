using System.Net;
using SongRequest.Config;
using SongRequest.SongPlayer;

namespace SongRequest.Handlers
{
    public class IndexHandler : StaticHandler
    {
		public IndexHandler (System.Reflection.Assembly resourceAssembly) :
			base(resourceAssembly)
		{
			
		}

        public override void Process(HttpListenerRequest request, HttpListenerResponse response)
        {
            SongPlayerFactory.GetConfigFile();

            string text = Get("index.htm");
            response.ContentType = "text/html";

            WriteUtf8String(response.OutputStream, text);
        }
    }
}
