using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using NetMQ;
using NetMQ.Sockets;
using System.Threading.Tasks;
using LightImage.FileSharing.Options;
using LightImage.FileSharing.Managers;
using System.Linq;
using LightImage.Networking.Services;
using Microsoft.Extensions.Logging;

namespace LightImage.FileSharing.Tests
{
    [TestClass]
    public class FileShareShimTest
    {
        private const string C_HOST = "127.0.0.1";
        private static string C_HASH = "0123456789abcdef0123456789abcdef";

        private static TimeSpan C_TIMEOUT = TimeSpan.FromMilliseconds(50);

        private NetMQActor _actor;
        private DealerSocket _dealer;
        private Mock<IDownloadManager> _downloads;
        private Guid _id;
        private ILoggerFactory _loggerFactory;
        private FileShareShim _shim;
        private Mock<IUploadManager> _uploads;

        [TestCleanup]
        public void Cleanup()
        {
            _dealer.Disconnect(_shim.RouterEndpoint);
            _dealer.Dispose();
            _actor.Dispose();
            NetMQConfig.Cleanup();
            _loggerFactory.Dispose();
        }

        [TestInitialize]
        public void Initialize()
        {
            _loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            _id = new Guid();
            var options = new FileShareOptions();
            _downloads = new Mock<IDownloadManager>();
            _uploads = new Mock<IUploadManager>();
            _shim = new FileShareShim(_id, C_HOST, options, _downloads.Object, _uploads.Object, _loggerFactory.CreateLogger<FileShareShim>());
            _actor = NetMQActor.Create(_shim);
            _dealer = new DealerSocket(_shim.RouterEndpoint);
            _dealer.Options.Identity = _id.ToIdentity();
        }

        [TestMethod]
        public void TestCommandAdd()
        {
            var descriptor = new FileDescriptor(1, C_HASH, 5L);
            FileShareCommands.SendAdd(_actor, descriptor, "test");
            Task.Delay(C_TIMEOUT).Wait();
            _uploads.Verify(um => um.Add(descriptor, "test"));
        }

        [TestMethod]
        public void TestCommandCancel()
        {
            var descriptor = new FileDescriptor(1, C_HASH, 5L);
            FileShareCommands.SendCancel(_actor, descriptor);
            Task.Delay(C_TIMEOUT).Wait();
            _downloads.Verify(dm => dm.Cancel(descriptor, _shim));
        }

        [TestMethod]
        public void TestCommandRemove()
        {
            var descriptor = new FileDescriptor(1, C_HASH, 5L);
            FileShareCommands.SendRemove(_actor, descriptor);
            Task.Delay(C_TIMEOUT).Wait();
            _uploads.Verify(um => um.Remove(descriptor, _shim));
        }

        [TestMethod]
        public void TestCommandRequest()
        {
            var descriptor = new FileDescriptor(1, C_HASH, 5L);
            FileShareCommands.SendRequest(_actor, descriptor, "test");
            Task.Delay(C_TIMEOUT).Wait();
            _downloads.Verify(dm => dm.Request(descriptor, "test", _shim));
        }

        [TestMethod]
        public void TestMessageChunk()
        {
            var descriptor = new FileDescriptor(1, C_HASH, 5L);
            var range = new ChunkRange(1, 3, 3);
            var data = new byte[] { 1, 2, 3 };
            FileShareMessages.SendChunk(_dealer, descriptor, range, data);
            Task.Delay(C_TIMEOUT).Wait();
            _downloads.Verify(dm => dm.HandleChunk(descriptor, range, It.Is<byte[]>(b => b.Take(3).SequenceEqual(data)), _id, _shim));
        }

        [TestMethod]
        public void TestMessageGet()
        {
            var descriptor = new FileDescriptor(1, C_HASH, 5L);
            var range = new ChunkRange(1, 128, 128);
            FileShareMessages.SendGet(_dealer, descriptor, range);
            Task.Delay(C_TIMEOUT).Wait();
            _uploads.Verify(um => um.HandleGet(descriptor, range, _id, _shim));
        }

        [TestMethod]
        public void TestMessageMissing()
        {
            var descriptor = new FileDescriptor(1, C_HASH, 5L);
            var range = new ChunkRange(1, 128, 128);
            FileShareMessages.SendMissing(_dealer, descriptor, range);
            Task.Delay(C_TIMEOUT).Wait();
            _downloads.Verify(dm => dm.HandleMissing(descriptor, range, _id, _shim));
        }
    }
}