using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace SongRequest
{
    public class FairQueue
    {
        private List<RequestedSong> _requestedSongs = new List<RequestedSong>();
        
        public int Count
        {
            get
            {
                return _requestedSongs.Count;
            }
        }

        public void Add(RequestedSong requestedSong)
        {
            //Do not allow adding same song twice...
            if (_requestedSongs.Any(r => r.Song == requestedSong.Song))
                return;

            _requestedSongs.Add(requestedSong);
        }

        public void Remove(string id)
        {
            bool found = false;
            _requestedSongs.RemoveAll(x =>
            {
                if (x.Song.TempId == id && !found)
                {
                    found = true;
                    return true;
                }
                return false;
            });
        }

        public void Remove(RequestedSong requestedSong)
        {
            Remove(requestedSong.Song.TempId);
        }

        public IEnumerable<RequestedSong> Current
        {
            get
            {
                Dictionary<string, int> requestsByUser = new Dictionary<string, int>();

                return _requestedSongs
                    .OrderBy(x => {
                        int currentAmount = requestsByUser.ContainsKey(x.RequesterName) ? requestsByUser[x.RequesterName] : 0;
                        return requestsByUser[x.RequesterName] = currentAmount+1;
                    })
                    .ThenBy(x => x.RequestedDate);
            }
        }
    }
}
