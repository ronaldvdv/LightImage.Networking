using LightImage.Networking.FileSharing.Algorithms;
using LightImage.Networking.FileSharing.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Linq;

namespace LightImage.Networking.FileSharing.Tests
{
    [TestClass]
    public class AlgorithmTest
    {
        private DownloadAlgorithm _algorithm;
        private FileShareOptions _options;

        [TestInitialize]
        public void Initialize()
        {
            _options = new FileShareOptions
            {
                ChunkSize = 10,
                MaxParallelChunks = 10,
                MaxParallelPerFile = 3,
                MaxParallelPerPeer = 5
            };
            _algorithm = new DownloadAlgorithm(_options);
        }

        [TestMethod]
        public void TestDontPickUnavailableOption()
        {
            var status = new Mock<IDownloadStatus>();
            var peer = Guid.NewGuid();
            var file = new FileDescriptor(1, FileDescriptor.C_EMPTY_HASH, 200);

            status.Setup(ds => ds.Peers).Returns(Enumerable.Repeat(peer, 1));
            status.Setup(ds => ds.Files).Returns(Enumerable.Repeat(file, 1));
            status.Setup(ds => ds.GetAvailability(file, peer)).Returns(Availability.Unavailable);

            Assert.IsFalse(_algorithm.Step(status.Object, out FileDescriptor outFile, out var outRange, out var outPeer));
        }

        [TestMethod]
        public void TestNothingWhenNoFiles()
        {
            var status = new Mock<IDownloadStatus>();
            var peer = Guid.NewGuid();
            status.Setup(ds => ds.Peers).Returns(new Guid[] { peer });
            status.Setup(ds => ds.Files).Returns(new FileDescriptor[] { });
            Assert.IsFalse(_algorithm.Step(status.Object, out var outFile, out var outRange, out var outPeer));
        }

        [TestMethod]
        public void TestNothingWhenNoPeers()
        {
            var status = new Mock<IDownloadStatus>();
            var file = new FileDescriptor(1, FileDescriptor.C_EMPTY_HASH, 200);
            status.Setup(ds => ds.Peers).Returns(new Guid[] { });
            status.Setup(ds => ds.Files).Returns(new FileDescriptor[] { file });
            Assert.IsFalse(_algorithm.Step(status.Object, out var outFile, out var outRange, out var outPeer));
        }

        [TestMethod]
        public void TestPickAnything()
        {
            var status = new Mock<IDownloadStatus>();
            var peer = Guid.NewGuid();
            var file1 = new FileDescriptor(1, FileDescriptor.C_EMPTY_HASH, 200);
            var file2 = new FileDescriptor(2, FileDescriptor.C_EMPTY_HASH, 200);

            status.Setup(ds => ds.Peers).Returns(Enumerable.Repeat(peer, 1));
            status.Setup(ds => ds.Files).Returns(new FileDescriptor[] { file1, file2 });
            status.Setup(ds => ds.GetWaitingChunks(It.IsAny<FileDescriptor>(), 1)).Returns(Enumerable.Range(0, 20));
            status.Setup(ds => ds.HasWaitingChunks(It.IsAny<FileDescriptor>())).Returns(true);

            Assert.IsTrue(_algorithm.Step(status.Object, out FileDescriptor outFile, out var outRange, out var outPeer));
        }

        /// <summary>
        /// While not too many small file requests are pending, give these priority.
        /// After that, prioritize the large file which has bad availability.
        /// </summary>
        /// <param name="nSmallChunksPending">Number of pending requests for the small file</param>
        /// <param name="expectedChoice">Number of the expected selected file (1=large, 2=small)</param>
        [TestMethod]
        [DataRow(2, 2)]
        [DataRow(3, 1)]
        [DataRow(4, 1)]
        public void TestPickLeastAvailable(int nSmallChunksPending, int expectedChoice)
        {
            var status = new Mock<IDownloadStatus>();
            var peer1 = Guid.NewGuid();
            var peer2 = Guid.NewGuid();
            var file1 = new FileDescriptor(1, FileDescriptor.C_EMPTY_HASH, DownloadAlgorithm.C_SMALL_THRESHOLD + 1);
            var file2 = new FileDescriptor(2, FileDescriptor.C_EMPTY_HASH, 200);

            status.Setup(ds => ds.Peers).Returns(new Guid[] { peer1, peer2 });
            status.Setup(ds => ds.Files).Returns(new FileDescriptor[] { file1, file2 });
            status.Setup(ds => ds.PendingRequests).Returns(Enumerable.Repeat(new ChunkRequest(file2, new ChunkRange(0, 0, 0), peer1, TimeSpan.Zero), nSmallChunksPending));
            status.Setup(ds => ds.GetWaitingChunks(It.IsAny<FileDescriptor>(), 1)).Returns(Enumerable.Range(0, 20));
            status.Setup(ds => ds.HasWaitingChunks(It.IsAny<FileDescriptor>())).Returns(true);
            status.Setup(ds => ds.GetAvailability(file1, It.IsAny<Guid>())).Returns(Availability.Available);
            status.Setup(ds => ds.GetAvailability(file1, peer1)).Returns(Availability.Unavailable);
            status.Setup(ds => ds.GetAvailability(file1, peer2)).Returns(Availability.Available);

            Assert.IsTrue(_algorithm.Step(status.Object, out FileDescriptor outFile, out var outRange, out var outPeer));
            Assert.AreEqual(expectedChoice, outFile.Id);
        }

        [TestMethod]
        public void TestPickNextChunk()
        {
            var status = new Mock<IDownloadStatus>();
            var peer = Guid.NewGuid();
            var file = new FileDescriptor(1, FileDescriptor.C_EMPTY_HASH, 200);

            status.Setup(ds => ds.Peers).Returns(Enumerable.Repeat(peer, 1));
            status.Setup(ds => ds.Files).Returns(Enumerable.Repeat(file, 1));
            status.Setup(ds => ds.GetWaitingChunks(It.IsAny<FileDescriptor>(), 1)).Returns(Enumerable.Range(10, 20));
            status.Setup(ds => ds.HasWaitingChunks(It.IsAny<FileDescriptor>())).Returns(true);

            Assert.IsTrue(_algorithm.Step(status.Object, out FileDescriptor outFile, out var outRange, out var outPeer));
            Assert.AreEqual(10, outRange.Index);
        }

        [TestMethod]
        public void TestPickSimple()
        {
            var status = new Mock<IDownloadStatus>();
            var peer = Guid.NewGuid();
            var file = new FileDescriptor(1, FileDescriptor.C_EMPTY_HASH, 200);

            status.Setup(ds => ds.Peers).Returns(Enumerable.Repeat(peer, 1));
            status.Setup(ds => ds.Files).Returns(Enumerable.Repeat(file, 1));
            status.Setup(ds => ds.GetWaitingChunks(It.IsAny<FileDescriptor>(), 1)).Returns(Enumerable.Range(0, 20));
            status.Setup(ds => ds.HasWaitingChunks(It.IsAny<FileDescriptor>())).Returns(true);

            Assert.IsTrue(_algorithm.Step(status.Object, out FileDescriptor outFile, out var outRange, out var outPeer));
            Assert.AreEqual(file, outFile);
            Assert.AreEqual(0, outRange.Index);
            Assert.AreEqual(10, outRange.Size);
            Assert.AreEqual(peer, outPeer);
        }

        [TestMethod]
        public void TestPickSmall()
        {
            var status = new Mock<IDownloadStatus>();
            var peer = Guid.NewGuid();
            var file1 = new FileDescriptor(1, FileDescriptor.C_EMPTY_HASH, DownloadAlgorithm.C_SMALL_THRESHOLD + 1);
            var file2 = new FileDescriptor(2, FileDescriptor.C_EMPTY_HASH, 200);

            status.Setup(ds => ds.Peers).Returns(Enumerable.Repeat(peer, 1));
            status.Setup(ds => ds.Files).Returns(new FileDescriptor[] { file1, file2 });
            status.Setup(ds => ds.GetWaitingChunks(It.IsAny<FileDescriptor>(), 1)).Returns(Enumerable.Range(0, 20));
            status.Setup(ds => ds.HasWaitingChunks(It.IsAny<FileDescriptor>())).Returns(true);

            Assert.IsTrue(_algorithm.Step(status.Object, out FileDescriptor outFile, out var outRange, out var outPeer));
            Assert.AreEqual(file2, outFile);
        }

        [TestMethod]
        public void TestPreferExpiredAvailabilityOption()
        {
            var status = new Mock<IDownloadStatus>();
            var peer1 = Guid.NewGuid();
            var peer2 = Guid.NewGuid();
            var peer3 = Guid.NewGuid();
            var file = new FileDescriptor(1, FileDescriptor.C_EMPTY_HASH, 200);

            status.Setup(ds => ds.Peers).Returns(new Guid[] { peer1, peer2, peer3 });
            status.Setup(ds => ds.Files).Returns(new FileDescriptor[] { file });
            status.Setup(ds => ds.GetAvailability(file, peer1)).Returns(Availability.Unavailable);
            status.Setup(ds => ds.GetAvailability(file, peer2)).Returns(Availability.Available);
            status.Setup(ds => ds.GetAvailability(file, peer3)).Returns(Availability.AvailabilityExpired);
            status.Setup(ds => ds.GetWaitingChunks(It.IsAny<FileDescriptor>(), 1)).Returns(Enumerable.Range(0, 20));
            status.Setup(ds => ds.HasWaitingChunks(It.IsAny<FileDescriptor>())).Returns(true);

            Assert.IsTrue(_algorithm.Step(status.Object, out FileDescriptor outFile, out var outRange, out var outPeer));
            Assert.AreEqual(peer3, outPeer);
        }

        [TestMethod]
        [DataRow(4, true)]
        [DataRow(5, false)]
        [DataRow(6, false)]
        public void TestRespectFileBound(int nPending, bool expectedOutcome)
        {
            var status = new Mock<IDownloadStatus>();
            var peer = Guid.NewGuid();
            var file = new FileDescriptor(1, FileDescriptor.C_EMPTY_HASH, 200);
            _options.MaxParallelChunks = 6;
            _options.MaxParallelPerFile = 5;
            _options.MaxParallelPerPeer = 6;

            status.Setup(ds => ds.Peers).Returns(new Guid[] { peer });
            status.Setup(ds => ds.Files).Returns(new FileDescriptor[] { file });
            status.Setup(ds => ds.PendingRequests).Returns(Enumerable.Repeat(new ChunkRequest(file, new ChunkRange(0, 0, 0), peer, TimeSpan.Zero), nPending));
            status.Setup(ds => ds.GetWaitingChunks(It.IsAny<FileDescriptor>(), It.IsAny<int>())).Returns(Enumerable.Range(0, 20));
            status.Setup(ds => ds.HasWaitingChunks(It.IsAny<FileDescriptor>())).Returns(true);

            Assert.AreEqual(expectedOutcome, _algorithm.Step(status.Object, out FileDescriptor outFile, out var outRange, out var outPeer));
        }

        [TestMethod]
        [DataRow(3, true)]
        [DataRow(4, false)]
        [DataRow(5, false)]
        public void TestRespectPeerBounds(int nPending, bool expectedOutcome)
        {
            var status = new Mock<IDownloadStatus>();
            var peer = Guid.NewGuid();
            var file = new FileDescriptor(1, FileDescriptor.C_EMPTY_HASH, 200);
            _options.MaxParallelChunks = 5;
            _options.MaxParallelPerFile = 5;
            _options.MaxParallelPerPeer = 4;

            status.Setup(ds => ds.Peers).Returns(new Guid[] { peer });
            status.Setup(ds => ds.Files).Returns(new FileDescriptor[] { file });
            status.Setup(ds => ds.PendingRequests).Returns(Enumerable.Repeat(new ChunkRequest(file, new ChunkRange(0, 0, 0), peer, TimeSpan.Zero), nPending));
            status.Setup(ds => ds.GetWaitingChunks(It.IsAny<FileDescriptor>(), It.IsAny<int>())).Returns(Enumerable.Range(0, 20));
            status.Setup(ds => ds.HasWaitingChunks(It.IsAny<FileDescriptor>())).Returns(true);

            Assert.AreEqual(expectedOutcome, _algorithm.Step(status.Object, out FileDescriptor outFile, out var outRange, out var outPeer));
        }

        [TestMethod]
        [DataRow(2, true)]
        [DataRow(3, false)]
        [DataRow(4, false)]
        public void TestRespectTotalBound(int nPending, bool expectedOutcome)
        {
            var status = new Mock<IDownloadStatus>();
            var peer = Guid.NewGuid();
            var file = new FileDescriptor(1, FileDescriptor.C_EMPTY_HASH, 200);
            _options.MaxParallelChunks = 3;
            _options.MaxParallelPerFile = 4;
            _options.MaxParallelPerPeer = 4;

            status.Setup(ds => ds.Peers).Returns(new Guid[] { peer });
            status.Setup(ds => ds.Files).Returns(new FileDescriptor[] { file });
            status.Setup(ds => ds.PendingRequests).Returns(Enumerable.Repeat(new ChunkRequest(file, new ChunkRange(0, 0, 0), peer, TimeSpan.Zero), nPending));
            status.Setup(ds => ds.GetWaitingChunks(It.IsAny<FileDescriptor>(), It.IsAny<int>())).Returns(Enumerable.Range(0, 20));
            status.Setup(ds => ds.HasWaitingChunks(It.IsAny<FileDescriptor>())).Returns(true);

            Assert.AreEqual(expectedOutcome, _algorithm.Step(status.Object, out FileDescriptor outFile, out var outRange, out var outPeer));
        }
    }
}