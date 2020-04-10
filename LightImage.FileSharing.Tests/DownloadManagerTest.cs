using LightImage.FileSharing.Algorithms;
using LightImage.FileSharing.IO;
using LightImage.FileSharing.Managers;
using LightImage.FileSharing.Options;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Linq;

namespace LightImage.FileSharing.Tests
{
    [TestClass]
    public class DownloadManagerTest
    {
        private Mock<IDownloadAlgorithm> _algorithm = new Mock<IDownloadAlgorithm>();
        private Mock<IDownloadContext> _context = new Mock<IDownloadContext>();
        private FileDescriptor _file;
        private ILoggerFactory _loggerFactory;
        private DownloadManager _manager;
        private FileShareOptions _options;
        private Guid _peer;
        private Mock<IChunkWriter> _writer = new Mock<IChunkWriter>();

        private delegate void Callback(IDownloadStatus s, ref FileDescriptor f, ref ChunkRange r, ref Guid p);

        [TestCleanup]
        public void Cleanup()
        {
            _loggerFactory.Dispose();
        }

        [TestInitialize]
        public void Initialize()
        {
            _loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var logger = _loggerFactory.CreateLogger<DownloadManager>();
            _options = new FileShareOptions { ChunkSize = 10 };
            _manager = new DownloadManager(_options, _writer.Object, _algorithm.Object, logger);
            _file = new FileDescriptor(1, FileDescriptor.C_EMPTY_HASH, 200);
            _peer = Guid.NewGuid();
        }

        [TestMethod]
        public void TestAddPeerCausesStep()
        {
            SetupAlgorithm(_file, _peer, 0);
            _manager.AddPeer(_peer, _context.Object);
            AssertAnyStep();
        }

        [TestMethod]
        public void TestCancelCausesStep()
        {
            SetupAlgorithm(_file, _peer, 0);
            _manager.Request(_file, "test", _context.Object);
            _context.Invocations.Clear();
            _manager.Cancel(_file, _context.Object);
            AssertAnyStep();
        }

        [TestMethod]
        public void TestCancelRemovesDownload()
        {
            // Request a file
            SetupAlgorithm(_file, _peer, 0);
            _manager.Request(_file, "test", _context.Object);

            // Now clear and cancel
            _context.Invocations.Clear();
            _manager.Cancel(_file, _context.Object);

            // The pending request should be removed
            Assert.AreEqual(0, _manager.PendingRequests.Count());
        }

        [TestMethod]
        public void TestChunkCallsWriterAndStep()
        {
            // Request a file; one GET will be sent
            _options.MaxParallelChunks = 1;
            SetupAlgorithm(_file, _peer, 4, 1);
            _manager.Request(_file, "test", _context.Object);
            Managers.DownloadProgressEventArgs progress = null;
            _manager.DownloadProgress += (sender, args) => progress = args;

            // Now clear and send the chunk
            _algorithm.Invocations.Clear();
            _context.Invocations.Clear();
            var data = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            _manager.HandleChunk(_file, new ChunkRange(0, 0, 10), data, _peer, _context.Object);

            // Make sure progress is reported
            Assert.IsNotNull(progress);
            Assert.AreEqual(_file, progress.Descriptor);
            Assert.AreEqual(10L, progress.BytesReceived);

            // Make sure the chunk is written and a new request is made
            _writer.Verify(w => w.Write("test", 0L, 10, data));
            AssertAnyStep();
            _context.Verify(dc => dc.SendRequest(_file, new ChunkRange(1, 10, 10), _peer));
        }

        /// <summary>
        /// Request a file, send all chunks; make sure the file is closed
        /// </summary>
        [TestMethod]
        public void TestLastChunkCallsWriterClose()
        {
            _options.MaxParallelChunks = 20;
            _options.MaxParallelPerFile = 20;
            _options.MaxParallelPerPeer = 20;
            SetupAlgorithm(_file, _peer, 20);
            FileDescriptorEventArgs completed = null;
            _manager.DownloadCompleted += (sender, args) => completed = args;
            _manager.Request(_file, "test", _context.Object);

            _algorithm.Invocations.Clear();
            var data = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            for (int i = 0; i < 20; i++)
            {
                _writer.Invocations.Clear();
                _algorithm.Invocations.Clear();
                _context.Invocations.Clear();
                _manager.HandleChunk(_file, new ChunkRange(i, i * 10, 10), data, _peer, _context.Object);
            }
            _writer.Verify(w => w.Write("test", 190L, 10, data));
            _writer.Verify(w => w.Close("test"));
            _context.VerifyNoOtherCalls();
            Assert.AreEqual(0, _manager.PendingRequests.Count());
            Assert.AreEqual(0, _manager.Files.Count());

            // Make sure completion is reported
            Assert.IsNotNull(completed);
            Assert.AreEqual(_file, completed.Descriptor);
        }

        [TestMethod]
        public void TestMissingChunkMarksUnavailable()
        {
            // Request a file
            SetupAlgorithm(_file, _peer, 2);
            _manager.Request(_file, "test", _context.Object);

            // Now clear and send a MISSING message
            _algorithm.Invocations.Clear();
            _context.Invocations.Clear();
            _manager.HandleMissing(_file, new ChunkRange(0, 0, 10), _peer, _context.Object);

            // The file should be marked as unavailable at the peer, but still in the queue
            Assert.AreEqual(1, _manager.Files.Count());
            Assert.AreEqual(Availability.Unavailable, _manager.GetAvailability(_file, _peer));

            // Also, no other requests should be sent since we know the file is unavailable
            _context.VerifyNoOtherCalls();
            _writer.VerifyNoOtherCalls();
            Assert.AreEqual(0, _manager.PendingRequests.Count());
        }

        [TestMethod]
        public void TestRequestAddsDownload()
        {
            SetupAlgorithm(_file, _peer, 1);
            _manager.Request(_file, "test", _context.Object);
            Assert.AreEqual(0.0, _manager.GetProgress(_file));
            Assert.AreEqual(1, _manager.PendingRequests.Count());
            var request = _manager.PendingRequests.First();
            Assert.AreEqual(_file, request.Descriptor);
            Assert.AreEqual(_peer, request.Peer);
        }

        [TestMethod]
        public void TestRequestCausesStep()
        {
            SetupAlgorithm(_file, _peer, 0);
            _manager.Request(_file, "test", _context.Object);
            AssertAnyStep();
        }

        [TestMethod]
        public void TestStepCausesMessage()
        {
            SetupAlgorithm(_file, _peer, 3);
            _manager.Request(_file, "test", _context.Object);
            _context.Verify(ds => ds.SendRequest(_file, It.IsAny<ChunkRange>(), _peer), Times.Exactly(3));
        }

        private void AssertAnyStep()
        {
            _algorithm.Verify(a => a.Step(It.IsAny<IDownloadStatus>(), out It.Ref<FileDescriptor>.IsAny, out It.Ref<ChunkRange>.IsAny, out It.Ref<Guid>.IsAny));
        }

        private void SetupAlgorithm(FileDescriptor file, Guid peer, int max = 0, int breakAtSpecific = -1)
        {
            int i = 0;
            bool didBreak = false, result = false;
            _algorithm.Setup(dm => dm.Step(It.IsAny<IDownloadStatus>(), out It.Ref<FileDescriptor>.IsAny, out It.Ref<ChunkRange>.IsAny, out It.Ref<Guid>.IsAny))
                .Callback(new Callback((IDownloadStatus s, ref FileDescriptor of, ref ChunkRange or, ref Guid op) =>
                {
                    of = file;
                    or = new ChunkRange(i, i * 10, 10);
                    op = peer;
                    result = i < max;
                    if (i != breakAtSpecific || didBreak)
                        i++;
                    else if (i == breakAtSpecific)
                    {
                        didBreak = true;
                        result = false;
                    }
                }))
                .Returns(() => result);
        }
    }
}