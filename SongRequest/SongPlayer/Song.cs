using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;

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
                    _id =  BitConverter.ToString(sha1.ComputeHash(Encoding.UTF8.GetBytes(FileName))).Replace("-", "");
                }

                return _id;
            }
        }
        public string Artist { get; set; }
        public string Name { get; set; }
        public string FileName { get; set; }
        public int? Duration { get; set; }
        public bool TagRead { get; set; }
    }
}
