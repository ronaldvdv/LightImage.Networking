using LightImage.Networking.FileSharing.IO;
using LightImage.Networking.FileSharing.Managers;
using LightImage.Networking.FileSharing.Options;
using LightImage.Networking.FileSharing.Policies;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LightImage.Networking.FileSharing.Tests
{
    [TestClass]
    public class UploadManagerTest
    {
        private static readonly TimeSpan _smallTimeout = TimeSpan.FromMilliseconds(50);
        private readonly Mock<IUploadContext> _context = new Mock<IUploadContext>();
        private readonly Mock<IChunkReader> _reader = new Mock<IChunkReader>();
        
        private ILogger<UploadManager> _logger;
        private ILoggerFactory _loggerFactory;
        
        [TestCleanup]
        public void Cleanup()
        {
            _loggerFactory.Dispose();
        }

        [TestInitialize]
        public void Initialize()
        {
            _loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            _logger = _loggerFactory.CreateLogger<UploadManager>();
        }

        [TestMethod]
        public void TestDoNotRetryEarly()
        {
            var options = new FileShareOptions { UploadRetryPolicy = RetryPolicyConfig.Constant(1, _smallTimeout) };
            var manager = new UploadManager(options, _reader.Object, _logger);
            _reader.Setup(cr => cr.Read(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<byte[]>())).Throws<IOException>();

            var id = Guid.NewGuid();
            var descriptor = new FileDescriptor(1, FileDescriptor.C_EMPTY_HASH, 5);
            var range = new ChunkRange(0, 0, 5);

            // Handle GET and make sure nothing is replied yet
            manager.Add(descriptor, "test");
            manager.HandleGet(descriptor, range, id, _context.Object);
            _context.VerifyNoOtherCalls();
            _reader.Reset();

            // Handle timer immediately and make sure nothing is tried yet
            manager.HandleTimer(_context.Object);
            _reader.VerifyNoOtherCalls();
            _context.VerifyNoOtherCalls();

            // Now wait before handling the timer
            Task.Delay(_smallTimeout).Wait();
            manager.HandleTimer(_context.Object);
            _reader.Verify(cr => cr.Read("test", 0, 5, It.IsAny<byte[]>()));
        }

        [TestMethod]
        public void TestGiveUpAfterMaxRetries()
        {
            const int C_MAX_ATTEMPTS = 3;

            var options = new FileShareOptions { UploadRetryPolicy = RetryPolicyConfig.Immediate(C_MAX_ATTEMPTS) };
            var manager = new UploadManager(options, _reader.Object, _logger);
            _reader.Setup(cr => cr.Read(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<byte[]>())).Throws<IOException>();

            var id = Guid.NewGuid();
            var descriptor = new FileDescriptor(1, FileDescriptor.C_EMPTY_HASH, 5);
            var range = new ChunkRange(0, 0, 5);

            // Handle GET and make sure nothing is replied yet
            manager.Add(descriptor, "test");
            manager.HandleGet(descriptor, range, id, _context.Object);
            _context.VerifyNoOtherCalls();

            // Now handle timer MaxRetries-1 times and make sure nothing is replied yet
            for (int i = 0; i < C_MAX_ATTEMPTS - 1; i++)
                manager.HandleTimer(_context.Object);
            _context.VerifyNoOtherCalls();

            // Handle the timer one more time; now a MISSING must be sent
            manager.HandleTimer(_context.Object);
            _context.Verify(uc => uc.SendMissing(descriptor, range, id));
        }

        [TestMethod]
        public void TestRequestNormal()
        {
            var options = new FileShareOptions { UploadRetryPolicy = RetryPolicyConfig.Immediate() };
            var manager = new UploadManager(options, _reader.Object, _logger);
            _reader.Setup(cr => cr.Read(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<byte[]>())).Callback<string, long, int, byte[]>((path, offset, size, buffer) => { new byte[] { 1, 2, 3 }.CopyTo(buffer, 0); }).Returns(3);

            var id = Guid.NewGuid();
            var descriptor = new FileDescriptor(1, FileDescriptor.C_EMPTY_HASH, 5);
            var range = new ChunkRange(0, 0, 5);

            // Handle GET and make sure CHUNK is replied
            manager.Add(descriptor, "test");
            manager.HandleGet(descriptor, range, id, _context.Object);
            _context.Verify(uc => uc.SendChunk(descriptor, new ChunkRange(0, 0, 3), id, It.Is<byte[]>(b => b.Take(3).SequenceEqual(new byte[] { 1, 2, 3 }))));
        }

        [TestMethod]
        public void TestRequestUnknown()
        {
            var options = new FileShareOptions { UploadRetryPolicy = RetryPolicyConfig.Immediate() };
            var manager = new UploadManager(options, _reader.Object, _logger);
            var id = Guid.NewGuid();
            var descriptor = new FileDescriptor(1, FileDescriptor.C_EMPTY_HASH, 5);
            var range = new ChunkRange(0, 0, 5);

            // Handle GET and make sure MISSING is replied
            manager.HandleGet(descriptor, range, id, _context.Object);
            _context.Verify(uc => uc.SendMissing(descriptor, range, id));

            // Make sure later Timer events don't send additional messages
            _context.Reset();
            manager.HandleTimer(_context.Object);
            _context.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void TestRetryOnce()
        {
            var options = new FileShareOptions { UploadRetryPolicy = RetryPolicyConfig.Immediate() };
            var manager = new UploadManager(options, _reader.Object, _logger);
            _reader.SetupSequence(cr => cr.Read(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<byte[]>()))
                .Throws<IOException>()
                .Returns(2);

            var id = Guid.NewGuid();
            var descriptor = new FileDescriptor(1, FileDescriptor.C_EMPTY_HASH, 5);
            var range = new ChunkRange(0, 0, 5);

            // Handle GET and make sure nothing is replied yet
            manager.Add(descriptor, "test");
            manager.HandleGet(descriptor, range, id, _context.Object);
            _context.VerifyNoOtherCalls();

            // Now handle timer and make sure the chunk is setn
            manager.HandleTimer(_context.Object);
            _context.Verify(uc => uc.SendChunk(descriptor, new ChunkRange(0, 0, 2), id, It.IsAny<byte[]>()));
        }

        [TestMethod]
        public void TestSendMissingWhenFileRemoved()
        {
            var options = new FileShareOptions { UploadRetryPolicy = RetryPolicyConfig.Immediate() };
            var manager = new UploadManager(options, _reader.Object, _logger);
            _reader.Setup(cr => cr.Read(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<byte[]>())).Throws<IOException>();

            var id = Guid.NewGuid();
            var descriptor = new FileDescriptor(1, FileDescriptor.C_EMPTY_HASH, 5);
            var range = new ChunkRange(0, 0, 5);

            // Handle GET and make sure nothing is replied yet
            manager.Add(descriptor, "test");
            manager.HandleGet(descriptor, range, id, _context.Object);
            _context.VerifyNoOtherCalls();

            // Now remove file
            manager.Remove(descriptor, _context.Object);

            // Now handle timer and make sure MISSING is sent
            manager.HandleTimer(_context.Object);
            _context.Verify(uc => uc.SendMissing(descriptor, range, id));

            // Add back the file and make sure no more attempts are made
            _reader.Reset();
            manager.Add(descriptor, "test");
            manager.HandleTimer(_context.Object);
            _reader.VerifyNoOtherCalls();
            _context.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void TestStopRetryingAfterRemove()
        {
            var options = new FileShareOptions { UploadRetryPolicy = RetryPolicyConfig.Immediate() };
            var manager = new UploadManager(options, _reader.Object, _logger);
            _reader.SetupSequence(cr => cr.Read(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<byte[]>()))
                .Throws<IOException>()
                .Returns(0);

            var id = Guid.NewGuid();
            var descriptor = new FileDescriptor(1, FileDescriptor.C_EMPTY_HASH, 5);
            var range = new ChunkRange(0, 0, 5);

            // Handle GET and make sure nothing is replied yet
            manager.Add(descriptor, "test");
            manager.HandleGet(descriptor, range, id, _context.Object);
            _context.VerifyNoOtherCalls();

            // Now remove; we expect an immediate MISSING
            manager.Remove(descriptor, _context.Object);
            _context.Verify(uc => uc.SendMissing(descriptor, range, id));

            // Now handle timer and make sure nothing else is sent
            manager.HandleTimer(_context.Object);
            _context.VerifyNoOtherCalls();
        }
    }
}