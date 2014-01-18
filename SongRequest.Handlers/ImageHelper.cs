using System;
using System.IO;
using System.Net;
using SongRequest.SongPlayer;
using System.Reflection;

namespace SongRequest.Handlers
{
    public class ImageHelper
    {
        private static byte[] _emptyImageLarge = null;
        private static byte[] _emptyImageSmall = null;

        private static byte[] _lastImage = null;
        private static string _lastId = null;

        private static object _lockObject = new object();

        public static void HelpMe(HttpListenerResponse response, string tempId, ISongPlayer songPlayer, bool large)
        {
            lock (_lockObject)
            {
                // if no temp id, return
                if (string.IsNullOrEmpty(tempId))
                {
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    return;
                }

                // use cached image if possible
                if (large && tempId.Equals(_lastId, StringComparison.OrdinalIgnoreCase) && _lastImage != null)
                {
                    WriteImage(response, _lastImage);
                    return;
                }

                // get from player
                MemoryStream imageStream;
                try
                {
                    imageStream = songPlayer.GetImageStream(tempId, large);
                }
                catch (Exception)
                {
                    imageStream = null;
                }

                using (MemoryStream streamFromSongPlayer = imageStream)
                {
                    if (streamFromSongPlayer != null)
                    {
                        // set last id if large image
                        if (large)
                        {
                            _lastId = tempId;
                            _lastImage = streamFromSongPlayer.ToArray();
                            WriteImage(response, _lastImage);
                        }
                        else
                        {
                            WriteImage(response, streamFromSongPlayer);
                        }
                        return;
                    }
                }

                // cache large if not present
                if (large && _emptyImageLarge == null)
                {
                    using (Stream stream = Assembly.GetEntryAssembly().GetManifestResourceStream("SongRequest.Static.empty.png"))
                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        stream.CopyTo(memoryStream);
                        _emptyImageLarge = memoryStream.ToArray();
                    }
                }
                // cache small if not present
                else if (!large && _emptyImageSmall == null)
                {
                    using (Stream stream = Assembly.GetEntryAssembly().GetManifestResourceStream("SongRequest.Static.empty_small.png"))
                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        stream.CopyTo(memoryStream);
                        _emptyImageSmall = memoryStream.ToArray();
                    }
                }

                if (large)
                {
                    WriteImage(response, _emptyImageLarge);
                }
                else
                {
                    WriteImage(response, _emptyImageSmall);
                }
            }
        }

        private static void WriteImage(HttpListenerResponse response, MemoryStream streamToCopy)
        {
            response.StatusCode = (int)HttpStatusCode.OK;
            response.ContentLength64 = streamToCopy.Length;
            response.ContentType = "image/png";

            // copy to response
            streamToCopy.CopyTo(response.OutputStream);
        }

        private static void WriteImage(HttpListenerResponse response, byte[] bytes)
        {
            using (MemoryStream memoryStream = new MemoryStream(bytes))
            {
                memoryStream.Position = 0;
                WriteImage(response, memoryStream);
            }
        }
    }
}
