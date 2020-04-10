using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LightImage.Networking.FileSharing.Tests
{
    [TestClass]
    public class ChunkMapTest
    {
        [TestMethod]
        public void TestAddPending()
        {
            var map = new ChunkMap(5);
            map.Set(2, ChunkState.Pending);
            Assert.AreEqual(5, map.Count);
            Assert.AreEqual(4, map.Waiting);
            Assert.AreEqual(1, map.Pending);
            Assert.AreEqual(0, map.Received);
            Assert.AreEqual(ChunkState.Pending, map[2]);
        }

        [TestMethod]
        public void TestAddReceived()
        {
            var map = new ChunkMap(3);
            map.Set(1, ChunkState.Pending);
            map.Set(1, ChunkState.Received);
            Assert.AreEqual(3, map.Count);
            Assert.AreEqual(2, map.Waiting);
            Assert.AreEqual(0, map.Pending);
            Assert.AreEqual(1, map.Received);
            Assert.AreEqual(ChunkState.Received, map[1]);
        }

        [TestMethod]
        public void TestCancelPending()
        {
            var map = new ChunkMap(3);
            map.Set(1, ChunkState.Pending);
            map.Set(1, ChunkState.Waiting);
            Assert.AreEqual(3, map.Count);
            Assert.AreEqual(3, map.Waiting);
            Assert.AreEqual(0, map.Pending);
            Assert.AreEqual(0, map.Received);
            Assert.AreEqual(ChunkState.Waiting, map[1]);
        }

        [TestMethod]
        public void TestEmpty()
        {
            var map = new ChunkMap(10);
            Assert.AreEqual(10, map.Count);
            Assert.AreEqual(10, map.Waiting);
            Assert.AreEqual(0, map.Pending);
            Assert.AreEqual(0, map.Received);
            Assert.AreEqual(ChunkState.Waiting, map[1]);
        }

        [TestMethod]
        public void TestFindFewerWaitingChunks()
        {
            // R-P-W-R
            var map = new ChunkMap(4);
            map.Set(0, ChunkState.Pending);
            map.Set(0, ChunkState.Received);
            map.Set(1, ChunkState.Pending);
            map.Set(3, ChunkState.Pending);
            map.Set(3, ChunkState.Received);
            var chunks = map.FindWaitingChunks(3).ToArray();
            Assert.AreEqual(1, chunks.Length);
            Assert.AreEqual(2, chunks[0]);
        }

        [TestMethod]
        public void TestFindNoWaitingChunks()
        {
            var map = new ChunkMap(3);
            for (int i = 0; i < map.Count; i++)
            {
                map.Set(i, ChunkState.Pending);
                map.Set(i, ChunkState.Received);
            }
            var chunks = map.FindWaitingChunks(3);
            Assert.AreEqual(0, chunks.Count());
        }

        [TestMethod]
        public void TestFindWaitingChunks()
        {
            // R-W-P-W-R-W-P-W
            var map = new ChunkMap(8);
            map.Set(0, ChunkState.Pending);
            map.Set(0, ChunkState.Received);
            map.Set(2, ChunkState.Pending);
            map.Set(4, ChunkState.Pending);
            map.Set(4, ChunkState.Received);
            map.Set(6, ChunkState.Pending);
            var waiting = map.FindWaitingChunks(3).ToArray();
            Assert.AreEqual(3, waiting.Length);
            Assert.AreEqual(1, waiting[0]);
            Assert.AreEqual(3, waiting[1]);
            Assert.AreEqual(5, waiting[2]);
        }

        [TestMethod]
        public void TestIndexAccessor()
        {
            var map = new ChunkMap(5);
            Assert.AreEqual(ChunkState.Waiting, map[0]);
            map[0] = ChunkState.Pending;
            Assert.AreEqual(ChunkState.Pending, map[0]);
            map[0] = ChunkState.Received;
            Assert.AreEqual(ChunkState.Received, map[0]);
        }

        [TestMethod]
        public void TestThrowFromWaitingToReceived()
        {
            var map = new ChunkMap(1);
            Assert.ThrowsException<InvalidChunkTransitionException>(() => map.Set(0, ChunkState.Received));
        }

        [TestMethod]
        public void TestThrowsFromReceivedToWaitingOrPending()
        {
            var map = new ChunkMap(1);
            map.Set(0, ChunkState.Pending);
            map.Set(0, ChunkState.Received);
            Assert.ThrowsException<InvalidChunkTransitionException>(() => map.Set(0, ChunkState.Pending));
            Assert.ThrowsException<InvalidChunkTransitionException>(() => map.Set(0, ChunkState.Waiting));
        }
    }
}