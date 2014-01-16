using System.Collections.Generic;
using System.IO;

namespace SongRequest.SongPlayer
{
    /// <summary>
    /// Event handler
    /// </summary>
    /// <param name="status"></param>
    public delegate void StatusChangedEventHandler(string status);

    /// <summary>
    /// Songplayer event
    /// </summary>
    public interface ISongplayer
    {
        /// <summary>
        /// Library status changed
        /// </summary>
        event StatusChangedEventHandler LibraryStatusChanged;

        /// <summary>
        /// Player status changed
        /// </summary>
        event StatusChangedEventHandler PlayerStatusChanged;

        /// <summary>
        /// Current player status
        /// </summary>
        PlayerStatus PlayerStatus { get; }

        /// <summary>
        /// Get playlist
        /// </summary>
        IEnumerable<Song> GetPlayList(string filter, string sortBy, bool ascending);

        /// <summary>
        /// Get image stream
        /// </summary>
        MemoryStream GetImageStream(string tempId, bool large);

        /// <summary>
        /// Play queue
        /// </summary>
        IEnumerable<RequestedSong> PlayQueue { get; }

        /// <summary>
        /// Enqueue song
        /// </summary>
        void Enqueue(string id, string requesterName);

        /// <summary>
        /// Enqueue song
        /// </summary>
        void Enqueue(Song song, string requesterName);

        /// <summary>
        /// Dequeue song
        /// </summary>
        void Dequeue(string id, string requesterName);

        /// <summary>
        /// Dequeue song
        /// </summary>
        void Dequeue(Song song, string requesterName);

        /// <summary>
        /// Pause song playing
        /// </summary>
        void Pause();

        /// <summary>
        /// Plan next song
        /// </summary>
        void Next(string requester);

        /// <summary>
        /// Rescan complete library
        /// </summary>
        void Rescan();

        /// <summary>
        /// Volume of player
        /// </summary>
        int Volume { get; set; }
    }
}

