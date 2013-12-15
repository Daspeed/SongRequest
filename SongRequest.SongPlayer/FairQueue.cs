using System;
using System.Collections.Generic;
using System.Linq;

namespace SongRequest.SongPlayer
{
    public class FairQueue
    {
        /// <summary>
        /// Lock object
        /// </summary>
        private static object lockObject = new object();

        /// <summary>
        /// Requested songs queue
        /// </summary>
        private List<RequestedSong> _requestedSongs = new List<RequestedSong>();

        /// <summary>
        /// Number of requested songs in queue, read in a lock
        /// </summary>
        public int Count
        {
            get
            {
                lock (lockObject)
                {
                    return _requestedSongs.Count;
                }
            }
        }

        /// <summary>
        /// Add song to queue
        /// </summary>
        public void Add(RequestedSong requestedSong)
        {
            lock (lockObject)
            {
                //Do not allow adding same song twice...
                if (_requestedSongs.Any(r => r.Song == requestedSong.Song))
                    return;

                for (int i = 1; i < _requestedSongs.Count; i++)
                {
                    if (_requestedSongs[i].RequesterName == requestedSong.RequesterName)
                        continue;

                    var groupedRequestedSongs = _requestedSongs.Take(i + 1).GroupBy(r => r.RequesterName).OrderByDescending(g => g.Count());

                    var maxRequesterUntilNow = groupedRequestedSongs.FirstOrDefault();
                    if (maxRequesterUntilNow.First().RequesterName != requestedSong.RequesterName &&
                        maxRequesterUntilNow.Count() > 1 &&
                        (groupedRequestedSongs.FirstOrDefault(g => g.First().RequesterName == requestedSong.RequesterName) == null ||
                         groupedRequestedSongs.FirstOrDefault(g => g.First().RequesterName == requestedSong.RequesterName).Count() < (maxRequesterUntilNow.Count() - 1)))
                    {
                        _requestedSongs.Insert(i, requestedSong);
                        return;
                    }
                }

                _requestedSongs.Add(requestedSong);
            }
        }

        /// <summary>
        /// Remove song from queue
        /// </summary>
        /// <param name="id">Id of song to delete</param>
        public void Remove(string id, string requester, string currentSongId)
        {
            lock (lockObject)
            {
                List<RequestedSong> matches = _requestedSongs.Where(x => x.Song.TempId == id).ToList();
                foreach (RequestedSong requestedSong in matches)
                {
                    string combine = requester;
                    if (!requestedSong.Song.TempId.Equals(currentSongId))
                    {
                        // show it's from the queue and set 'play date' so we can define when skipped
                        combine += " (q)";
                        requestedSong.Song.LastPlayDateTime = DateTime.Now;
                    }

                    requestedSong.Song.SkippedBy = combine;
                }

                _requestedSongs.RemoveAll(x => x.Song.TempId == id);
            }
        }

        /// <summary>
        /// Remove song from queue
        /// </summary>
        /// <param name="requestedSong">Song to delete</param>
        public void Remove(RequestedSong requestedSong, string requester, string currentSongId)
        {
            Remove(requestedSong.Song.TempId, requester, currentSongId);
        }

        /// <summary>
        /// Current queue
        /// </summary>
        public IEnumerable<RequestedSong> Current
        {
            get
            {
                return _requestedSongs;
            }
        }
    }
}
