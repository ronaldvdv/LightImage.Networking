using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Linq;

namespace LightImage.Networking.Services.Tests
{
    [TestClass]
    public class ServiceManagerTests
    {
        private const string C_HOST = "1.2.3.108";
        private const string C_NAME = "MyName";
        private const string C_ROLE = "MyRole";
        private Mock<IService> _service;
        private int[] C_PORTS = new[] { 1, 2, 5 };

        [TestInitialize]
        public void Initialize()
        {
            _service = new Mock<IService>();
            _service.SetupGet(x => x.Name).Returns(C_NAME);
            _service.SetupGet(x => x.Role).Returns(C_ROLE);
            _service.SetupGet(x => x.Ports).Returns(C_PORTS);
        }

        [TestMethod]
        public void TestCannotUseServicesWithoutRunning()
        {
            var manager = new ServiceManager(new IService[] { });
            manager.Start();
            manager.Stop();
            Assert.ThrowsException<InvalidOperationException>(() => manager.Reset());
            Assert.ThrowsException<InvalidOperationException>(() => manager.Remove(Guid.NewGuid()));
            Assert.ThrowsException<InvalidOperationException>(() => manager.Connect(Guid.NewGuid(), "", "", new int[] { }, ""));
            Assert.ThrowsException<InvalidOperationException>(() => manager.Disconnect(Guid.NewGuid(), ""));
        }

        [TestMethod]
        public void TestConnectCallsService()
        {
            var id = Guid.NewGuid();

            var service2 = new Mock<IService>();
            service2.SetupGet(s => s.Name).Returns("Another service");

            var manager = new ServiceManager(new[] { _service.Object, service2.Object });
            manager.Start();
            manager.Connect(id, C_NAME, C_HOST, C_PORTS, C_ROLE);

            _service.Verify(s => s.Add(id, C_HOST, C_ROLE, C_PORTS));
            service2.Verify(s => s.Add(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int[]>()), Times.Never);
        }

        [TestMethod]
        public void TestDisconnectCallsService()
        {
            var id = Guid.NewGuid();

            var service2 = new Mock<IService>();
            service2.SetupGet(s => s.Name).Returns("Another service");

            var manager = new ServiceManager(new[] { _service.Object, service2.Object });
            manager.Start();
            manager.Disconnect(id, C_NAME);

            _service.Verify(s => s.Remove(id));
            service2.Verify(s => s.Remove(It.IsAny<Guid>()), Times.Never);
        }

        [TestMethod]
        public void TestGetDescriptorsCopyServiceData()
        {
            var manager = new ServiceManager(new[] { _service.Object });
            manager.Start();

            var descriptors = manager.GetDescriptors();
            var descriptor = descriptors.Single();

            Assert.AreEqual(C_NAME, descriptor.Name);
            Assert.AreEqual(C_ROLE, descriptor.Role);
            Assert.AreEqual(C_PORTS, descriptor.Ports);
        }

        [TestMethod]
        public void TestHeartbeatForwarded()
        {
            var id = Guid.NewGuid();
            var manager = new ServiceManager(new[] { _service.Object });
            ServicePeerHeartbeatEventArgs heartbeat = null;
            var data = new ServicePeerHeartbeatEventArgs(C_NAME, id);
            manager.PeerHeartbeat += (sender, args) => heartbeat = args;
            _service.Raise(s => s.PeerHeartbeat += null, data);
            Assert.AreEqual(C_NAME, heartbeat.ServiceName);
            Assert.AreEqual(id, heartbeat.PeerId);
        }

        [TestMethod]
        public void TestRemoveCallsServices()
        {
            var id = Guid.NewGuid();

            var service2 = new Mock<IService>();
            service2.SetupGet(s => s.Name).Returns("Another service");

            var manager = new ServiceManager(new[] { _service.Object, service2.Object });
            manager.Start();
            manager.Remove(id);

            _service.Verify(s => s.Remove(id));
            service2.Verify(s => s.Remove(id));
        }

        [TestMethod]
        public void TestResetCallsEachService()
        {
            var manager = new ServiceManager(new[] { _service.Object });
            manager.Start();
            manager.Reset();
            _service.Verify(s => s.Reset());
        }

        [TestMethod]
        public void TestStartCallsEachService()
        {
            var manager = new ServiceManager(new[] { _service.Object });
            manager.Start();
            Assert.IsTrue(manager.IsRunning);
            _service.Verify(s => s.Start());
        }

        [TestMethod]
        public void TestStartTwice()
        {
            var manager = new ServiceManager(new IService[] { _service.Object });
            manager.Start();
            manager.Start();
            _service.Verify(s => s.Start(), Times.Once);
        }

        [TestMethod]
        public void TestStopCallsEachService()
        {
            var manager = new ServiceManager(new[] { _service.Object });
            manager.Start();
            manager.Stop();
            _service.Verify(s => s.Stop());
            Assert.IsFalse(manager.IsRunning);
        }

        [TestMethod]
        public void TestStopTwice()
        {
            var manager = new ServiceManager(new IService[] { _service.Object });
            manager.Start();
            manager.Stop();
            manager.Stop();
            _service.Verify(s => s.Stop(), Times.Once);
        }
    }
}