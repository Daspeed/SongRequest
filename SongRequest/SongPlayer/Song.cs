using System;
using System.Security.Cryptography;
using System.Text;

namespace SongRequest
{
    [Serializable]
    public class Song
    {
        private string _id = null;

        public string TempId
        {
            get
            {
                if (_id == null)
                {
                    SHA1CryptoServiceProvider sha1 = new SHA1CryptoServiceProvider();
                    _id = BitConverter.ToString(sha1.ComputeHash(Encoding.UTF8.GetBytes(FileName))).Replace("-", "");
                }

                return _id;
            }
        }

        /// <summary>
        /// Artist of song
        /// </summary>
        public string Artist { get; set; }

        /// <summary>
        /// Name/title of song
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// File name
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Duration in seconds
        /// </summary>
        public int? Duration { get; set; }

        /// <summary>
        /// If true, error while reading tag
        /// </summary>
        public bool ErrorReadingTag { get; set; }

        /// <summary>
        /// If true, tag is read
        /// </summary>
        public bool TagRead { get; set; }

        /// <summary>
        /// File creation date
        /// </summary>
        public string DateCreated { get; set; }

        /// <summary>
        /// Get artist & title combined
        /// </summary>
        public string GetArtistAndTitle()
        {
            if (!string.IsNullOrEmpty(Artist) && !string.IsNullOrEmpty(Name))
                return Artist + " - " + Name;

            if (!string.IsNullOrEmpty(Name))
                return Name;

            return FileName;
        }
    }
}
