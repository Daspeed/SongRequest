using System;
using System.IO;
using System.Net;
using SongRequest.SongPlayer;
using System.Reflection;

namespace SongRequest.Handlers
{
    public class ImageHelper
    {
        static byte[] _emptyImageLarge = null;
        static byte[] _emptyImageSmall = null;

        static byte[] _lastImage = null;
        static string _lastId = null;

        public static void HelpMe(HttpListenerRequest request, HttpListenerResponse response, string tempId, ISongPlayer songPlayer, bool large)
        {
            // if no temp id, return
            if (string.IsNullOrEmpty(tempId))
            {
                response.StatusCode = (int)HttpStatusCode.NotFound;
                return;
            }

            MemoryStream streamToCopy = null;

            // use cached image if possible
            if (large && tempId.Equals(_lastId, StringComparison.OrdinalIgnoreCase) && _lastImage != null)
            {
                streamToCopy = new MemoryStream(_lastImage);
            }

            if (streamToCopy == null)
            {
                streamToCopy = songPlayer.GetImageStream(tempId, large);
                if (streamToCopy == null)
                {
                    if (large)
                    {
                        if (_emptyImageLarge == null)
                        {
                            // load empty image
                            string fullPath = Path.Combine(Environment.CurrentDirectory, "SongRequest.exe");
                            using (Stream stream = Assembly.LoadFile(fullPath).GetManifestResourceStream("SongRequest.Static.empty.png"))
                            using (MemoryStream memoryStream = new MemoryStream())
                            {
                                stream.CopyTo(memoryStream);

                                // cache image
                                _emptyImageLarge = memoryStream.ToArray();
                            }
                        }

                        // use cached image
                        streamToCopy = new MemoryStream(_emptyImageLarge);
                    }
                    else
                    {
                        if (_emptyImageSmall == null)
                        {
                            // load empty image
                            string fullPath = Path.Combine(Environment.CurrentDirectory, "SongRequest.exe");
                            using (Stream stream = Assembly.LoadFile(fullPath).GetManifestResourceStream("SongRequest.Static.empty_small.png"))
                            using (MemoryStream memoryStream = new MemoryStream())
                            {
                                stream.CopyTo(memoryStream);

                                // cache image
                                _emptyImageSmall = memoryStream.ToArray();
                            }
                        }

                        // use cached image
                        streamToCopy = new MemoryStream(_emptyImageSmall);
                    }
                }
                else
                {
                    // set last id if large image
                    if (large)
                    {
                        _lastId = tempId;
                        _lastImage = streamToCopy.ToArray();
                    }

                    // reset position
                    streamToCopy.Position = 0;
                }
            }

            // Save resized picture
            response.StatusCode = (int)HttpStatusCode.OK;
            response.ContentLength64 = streamToCopy.Length;
            response.ContentType = "image/png";

            // copy to response
            streamToCopy.CopyTo(response.OutputStream);
        }
    }
}
