using System;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;

namespace SongRequest.Handlers
{
    public class StaticHandler : BaseHandler
    {
		System.Reflection.Assembly _resourceAssembly;

		public StaticHandler (System.Reflection.Assembly resourceAssembly)
		{
			this._resourceAssembly = resourceAssembly;
		}

        public override void Process(HttpListenerRequest request, HttpListenerResponse response)
        {
            Match match = Regex.Match(request.RawUrl, "^/static/(.+)$");

            string resource = match.Groups[1].Value;

            string extension = resource.Substring(resource.LastIndexOf('.') + 1).ToLower();

            bool useStream = false;

            switch (extension)
            {
                case "css":
                    response.ContentType = "text/css";
                    break;

                case "png":
                    response.ContentType = "image/png";
                    useStream = true;
                    break;

                case "gif":
                    response.ContentType = "image/gif";
                    useStream = true;
                    break;

                case "htm":
                case "html":
                    response.ContentType = "text/html";
                    break;

                case "js":
                    response.ContentType = "application/x-javascript";
                    break;

                default:
                    response.ContentType = "text/plain";
                    break;
            }

            if (useStream)
            {
                using (var stream = GetStream(resource))
                {
                    stream.CopyTo(response.OutputStream);
                }
            }
            else
            {
                string text = Get(resource);
                WriteUtf8String(response.OutputStream, text);
            }
        }

        protected string Get(string name)
        {
            using (var stream = GetStream(name))
            {
                if (stream == null)
                {
                    Console.Error.WriteLine("Could not find static content: {0}", name);
                    return null;
                }
                using (var streamReader = new StreamReader(stream))
                    return streamReader.ReadToEnd();
            }
        }

        protected Stream GetStream(string name)
        {

            // When in debug mode, get files from hard disk instead of resources
#if DEBUG
#if __MonoCS__
			string path = Path.GetFullPath(Environment.CurrentDirectory + @"/../../Static/" + name);
#else
			string path = Path.GetFullPath(Environment.CurrentDirectory + @"\..\..\Static\" + name);
#endif
            return File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
#else
            string resourceName = name.Replace("/", ".");
            return _resourceAssembly.GetManifestResourceStream("SongRequest.Static." + resourceName);
#endif
        }
    }
}
