using System;
using System.Security.Cryptography;
using System.Text;

namespace SongRequest
{
    [Serializable]
    public class Song
    {
        public Song()
        {
            LastPlayTime = string.Empty;
        }

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
        /// Genre of song
        /// </summary>
        public string Genre { get; set; }

        /// <summary>
        /// Year of song
        /// </summary>
        public string Year { get; set; }

        /// <summary>
        /// Last time song is played
        /// </summary>
        public string LastPlayTime { get; private set; }

        /// <summary>
        /// If true, tag is read
        /// </summary>
        public bool IsDirty { get; set; }

        /// <summary>
        /// Name of last requester
        /// </summary>
        public string LastRequester { get; set; }

        /// <summary>
        /// Name of last requester
        /// </summary>
        public string _skippedBy;

        /// <summary>
        /// Name of last requester
        /// </summary>
        public string SkippedBy
        {
            get { return _skippedBy; }
            set
            {
                if (string.IsNullOrEmpty(value) || value.Equals("randomizer", StringComparison.OrdinalIgnoreCase))
                    _skippedBy = string.Empty;
                else
                    _skippedBy = value;
            }
        }

        /// <summary>
        /// Last play time of song
        /// </summary>
        public DateTime? LastPlayDateTime
        {
            get
            {
                return _lastPlayDateTime;
            }
            set
            {
                _lastPlayDateTime = value;
                if (_lastPlayDateTime != null)
                {
                    IsDirty = true;
                    LastPlayTime = _lastPlayDateTime.Value.ToString("yyyy-MM-dd HH:mm");
                }
                else if (!string.IsNullOrEmpty(LastPlayTime))
                {
                    IsDirty = true;
                    LastPlayTime = string.Empty;
                }
            }
        }
        private DateTime? _lastPlayDateTime;

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
