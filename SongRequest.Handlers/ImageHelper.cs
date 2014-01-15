using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using SongRequest.SongPlayer;
using SongRequest.Utils;
using System.Drawing;
using System.Reflection;

namespace SongRequest.Handlers
{
    public class ImageHelper
    {
        static byte[] _emptyImage = null;
        static byte[] _lastImage = null;
        static string _lastId = null;

        public static void HelpMe(HttpListenerRequest request, HttpListenerResponse response, string tempId, ISongplayer songPlayer)
        {
            // if no temp id, return
            if (string.IsNullOrEmpty(tempId))
                return;

            // use cached image if possible
            if (tempId.Equals(_lastId, StringComparison.OrdinalIgnoreCase) && _lastImage != null)
            {
                using (MemoryStream memoryStream = new MemoryStream(_lastImage))
                {
                    memoryStream.CopyTo(response.OutputStream);
                }

                // done
                return;
            }

            MemoryStream imageStream = songPlayer.GetImageStream(tempId);
            if (imageStream == null)
            {
                if (_emptyImage == null)
                {
                    // load empty image
                    string fullPath = Path.Combine(Environment.CurrentDirectory, "SongRequest.exe");
                    using (Stream stream = Assembly.LoadFile(fullPath).GetManifestResourceStream("SongRequest.Static.empty.png"))
                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        response.ContentType = "image/png";
                        stream.CopyTo(memoryStream);

                        // cache image
                        _emptyImage = memoryStream.ToArray();
                    }
                }

                // use cached image
                using (MemoryStream memoryStream = new MemoryStream(_emptyImage))
                {
                    memoryStream.CopyTo(response.OutputStream);
                }

                return;
            }

            // set last id
            _lastId = tempId;

            // create tumbnail
            int width = 300;
            int maxHeight = 300;

            Image thumbnail = Image.FromStream(imageStream);

            // Prevent using images internal thumbnail
            thumbnail.RotateFlip(System.Drawing.RotateFlipType.Rotate180FlipNone);
            thumbnail.RotateFlip(System.Drawing.RotateFlipType.Rotate180FlipNone);

            if (thumbnail.Width <= width)
            {
                width = thumbnail.Width;
            }

            int height = thumbnail.Height * width / thumbnail.Width;
            if (height > maxHeight)
            {
                // Resize with height instead
                width = thumbnail.Width * maxHeight / thumbnail.Height;
                height = maxHeight;
            }

            System.Drawing.Image NewImage = thumbnail.GetThumbnailImage(width, height, null, IntPtr.Zero);
            thumbnail.Dispose();

            // Save resized picture
            response.ContentType = "image/png";

            using (MemoryStream cacheStream = new MemoryStream())
            {
                // cache it
                NewImage.Save(cacheStream, System.Drawing.Imaging.ImageFormat.Png);
                _lastImage = cacheStream.ToArray();

                // copy to response
                cacheStream.Position = 0;
                cacheStream.CopyTo(response.OutputStream);
            }
        }
    }
}
