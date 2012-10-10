using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SongRequest;

namespace SongRequest.Test
{
    /// <summary>
    /// </summary>
    [TestClass]
    public class FairQueueTest
    {
        [TestMethod]
        public void TestFairQueue()
        {
            FairQueue queue = new FairQueue();

            var first = new RequestedSong() { RequestedDate = DateTime.Now, Song = new Song() { FileName="A.mp3"  }, RequesterName="A" };
            queue.Add(first);

            Assert.AreEqual(queue.Count, 1);
            Assert.IsNotNull(queue.Current.First());

            queue.Add(new RequestedSong() { RequestedDate = DateTime.Now.AddSeconds(1), Song = new Song() { FileName = "B.mp3" }, RequesterName = "A" });

            Assert.AreEqual(queue.Count, 2);
            Assert.IsNotNull(queue.Current.First());
            Assert.AreEqual("A.mp3", queue.Current.First().Song.FileName);

            queue.Add(new RequestedSong() { RequestedDate = DateTime.Now.AddSeconds(1), Song = new Song() { FileName = "C.mp3" }, RequesterName = "B" });

            Assert.AreEqual(queue.Count, 3);
            Assert.IsNotNull(queue.Current.First());
            Assert.AreEqual("A.mp3", queue.Current.First().Song.FileName);
            Assert.AreEqual("C.mp3", queue.Current.ElementAt(1).Song.FileName);
            Assert.AreEqual("B.mp3", queue.Current.ElementAt(2).Song.FileName);

            queue.Add(new RequestedSong() { RequestedDate = DateTime.Now.AddSeconds(1), Song = new Song() { FileName = "D.mp3" }, RequesterName = "C" });

            Assert.AreEqual(queue.Count, 4);
            Assert.IsNotNull(queue.Current.First());
            Assert.AreEqual("A.mp3", queue.Current.First().Song.FileName);
            Assert.AreEqual("C.mp3", queue.Current.ElementAt(1).Song.FileName);
            Assert.AreEqual("D.mp3", queue.Current.ElementAt(2).Song.FileName);
            Assert.AreEqual("B.mp3", queue.Current.ElementAt(3).Song.FileName);

            queue.Remove(first);

            Assert.AreEqual(queue.Count, 3);
            Assert.IsNotNull(queue.Current.First());
            Assert.AreEqual("B.mp3", queue.Current.First().Song.FileName);
            Assert.AreEqual("C.mp3", queue.Current.ElementAt(1).Song.FileName);
            Assert.AreEqual("D.mp3", queue.Current.ElementAt(2).Song.FileName);   
        }

        [TestMethod]
        public void TestManyUsers()
        {
            FairQueue queue = new FairQueue();

            DateTime dateTime = DateTime.Now;

            foreach (int userNumber in Enumerable.Range(1, 3))
            {
                foreach (int songForUser in Enumerable.Range(1, 3))
                {
                    queue.Add(new RequestedSong() {
                        RequestedDate = dateTime.AddDays(userNumber).AddMinutes(songForUser),
                        Song = new Song() { FileName = songForUser + "_Song.mp3" },
                        RequesterName =  userNumber + "_User" 
                    });
                }
            }

            RequestedSong[] requestedSongs = queue.Current.ToArray();

            Assert.AreEqual("1_User", requestedSongs[0].RequesterName);
            Assert.AreEqual("2_User", requestedSongs[1].RequesterName);
            Assert.AreEqual("3_User", requestedSongs[2].RequesterName);
            Assert.AreEqual("1_User", requestedSongs[3].RequesterName);
            Assert.AreEqual("2_User", requestedSongs[4].RequesterName);
            Assert.AreEqual("3_User", requestedSongs[5].RequesterName);
            Assert.AreEqual("1_User", requestedSongs[6].RequesterName);
            Assert.AreEqual("2_User", requestedSongs[7].RequesterName);
            Assert.AreEqual("3_User", requestedSongs[8].RequesterName);
        }
    }
}
