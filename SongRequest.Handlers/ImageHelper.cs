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
        public static void HelpMe(HttpListenerRequest request, HttpListenerResponse response, string tempId, ISongplayer songPlayer)
        {
            if (string.IsNullOrEmpty(tempId))
                return;


            else
            {
                MemoryStream imageStream = songPlayer.GetImageStream(tempId);

                if (imageStream == null)
                {
                    // load empty image
                    string fullPath = Path.Combine(Environment.CurrentDirectory, "SongRequest.exe");
                    using (var stream = Assembly.LoadFile(fullPath).GetManifestResourceStream("SongRequest.Static.empty.png"))
                    {
                        response.ContentType = "image/png";
                        stream.CopyTo(response.OutputStream);
                    }
                }
                else
                {
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
                    NewImage.Save(response.OutputStream, System.Drawing.Imaging.ImageFormat.Png);
                }
            }
        }
    }
}
