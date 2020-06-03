using LightImage.Networking.Services;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetMQ;
using System;
using System.Collections.Generic;
using System.Threading;

namespace LightImage.Networking.Discovery.Tests
{
    [TestClass]
    public class DiscoveryNodeInteractionTests
    {
        private readonly List<DiscoveryNode> _nodes = new List<DiscoveryNode>();
        private readonly TimeSpan C_SMALL_TIMEOUT = TimeSpan.FromSeconds(0.1);
        private ILogger<DiscoveryNode> _logger;
        private ILoggerFactory _loggerFactory;
        private DiscoveryOptions _options;
        private FakeService _service1;
        private FakeService _service2;
        private FakeService _service3;
        private ILogger<DiscoveryShim> _shimLogger;

        [TestCleanup]
        public void Cleanup()
        {
            foreach (var node in _nodes)
                node.Dispose();
            NetMQConfig.Cleanup();
            _loggerFactory.Dispose();
        }

        [TestInitialize]
        public void Initialize()
        {
            SynchronizationContext.SetSynchronizationContext(new TestSynchronizationContext());

            _options = new DiscoveryOptions
            {
                BeaconInterval = C_SMALL_TIMEOUT / 2,
                TimerInterval = C_SMALL_TIMEOUT / 4,
                EvasiveThreshold = C_SMALL_TIMEOUT,
                LostThreshold = C_SMALL_TIMEOUT * 2
            };
            _service1 = new FakeService("TestService", "Master", new int[] { 17 });
            _service2 = new FakeService("TestService", "Slave", new int[] { 17 });
            _service3 = new FakeService("TestService", "Other", new int[] { 17 });
            _loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            _logger = _loggerFactory.CreateLogger<DiscoveryNode>();
            _shimLogger = _loggerFactory.CreateLogger<DiscoveryShim>();
        }

        [TestMethod]
        public void TestAddPeerInServiceWhenItsJoiningSession()
        {
            var node1 = new DiscoveryNode(Guid.NewGuid(), "Test1", "test", _logger, _shimLogger, new ServiceManager(new IService[] { _service1 }), _options);
            var session = 1;
            node1.Join(session);

            var node2 = new DiscoveryNode(Guid.NewGuid(), "Test2", "test", _logger, _shimLogger, new ServiceManager(new IService[] { _service2 }), _options);
            node2.Join(session);

            AddNodes(node1, node2);

            Thread.Sleep(C_SMALL_TIMEOUT);
            Assert.AreEqual(1, _service1.Peers.Count);
            Assert.AreEqual(node1.Id, _service2.Peers[0].Id);
            Assert.AreEqual("Master", _service2.Peers[0].Role);
            Assert.AreEqual(1, _service2.Peers.Count);
            Assert.AreEqual(node2.Id, _service1.Peers[0].Id);
            Assert.AreEqual("Slave", _service1.Peers[0].Role);
        }

        [TestMethod]
        public void TestAddPeerWhenImJoiningSession()
        {
            var node1 = new DiscoveryNode(Guid.NewGuid(), "Test1", "test", _logger, _shimLogger, new ServiceManager(new IService[] { _service1 }), _options);
            var session = 1;
            var node2 = new DiscoveryNode(Guid.NewGuid(), "Test2", "test", _logger, _shimLogger, new ServiceManager(new IService[] { _service2 }), _options);
            AddNodes(node1, node2);

            node2.Join(session);
            node1.Join(session);

            Thread.Sleep(C_SMALL_TIMEOUT);
            Assert.AreEqual(1, _service1.Peers.Count);
            Assert.AreEqual(node1.Id, _service2.Peers[0].Id);
            Assert.AreEqual("Master", _service2.Peers[0].Role);
            Assert.AreEqual(1, _service2.Peers.Count);
            Assert.AreEqual(node2.Id, _service1.Peers[0].Id);
            Assert.AreEqual("Slave", _service1.Peers[0].Role);
        }

        [TestMethod]
        public void TestIgnoreIfNotInSession()
        {
            var node1 = new DiscoveryNode(Guid.NewGuid(), "Test1", "test", _logger, _shimLogger, new ServiceManager(new IService[] { _service1 }), _options);
            var node2 = new DiscoveryNode(Guid.NewGuid(), "Test2", "test", _logger, _shimLogger, new ServiceManager(new IService[] { _service2 }), _options);
            AddNodes(node1, node2);
            node2.Join(1);

            Thread.Sleep(C_SMALL_TIMEOUT);
            Assert.AreEqual(0, _service1.Peers.Count);
            Assert.AreEqual(0, _service2.Peers.Count);
        }

        [TestMethod]
        public void TestRemovePeerFromSessionWhenSomebodyLeavesSession()
        {
            var node1 = new DiscoveryNode(Guid.NewGuid(), "Test1", "test", _logger, _shimLogger, new ServiceManager(new IService[] { _service1 }), _options);
            var session = 1;
            var node2 = new DiscoveryNode(Guid.NewGuid(), "Test2", "test", _logger, _shimLogger, new ServiceManager(new IService[] { _service2 }), _options);
            AddNodes(node1, node2);

            node2.Join(session);
            node1.Join(session);
            node1.Leave();

            Thread.Sleep(C_SMALL_TIMEOUT);
            Assert.AreEqual(0, _service1.Peers.Count);
            Assert.AreEqual(0, _service2.Peers.Count);
        }

        [TestMethod]
        public void TestSwitchSession()
        {
            var node1 = new DiscoveryNode(Guid.NewGuid(), "Test1", "test", _logger, _shimLogger, new ServiceManager(new IService[] { _service1 }), _options);
            var node2 = new DiscoveryNode(Guid.NewGuid(), "Test2", "test", _logger, _shimLogger, new ServiceManager(new IService[] { _service2 }), _options);
            var node3 = new DiscoveryNode(Guid.NewGuid(), "Test3", "test", _logger, _shimLogger, new ServiceManager(new IService[] { _service3 }), _options);
            AddNodes(node1, node2, node3);

            var session1 = 1;
            var session2 = 2;

            node2.Join(session1);
            node3.Join(session2);

            node1.Join(session1);
            node1.Join(session2);
            Thread.Sleep(C_SMALL_TIMEOUT * 2);

            Assert.AreEqual(1, _service1.Peers.Count);
            Assert.AreEqual(0, _service2.Peers.Count);
            Assert.AreEqual(1, _service3.Peers.Count);

            Assert.AreEqual(node3.Id, _service1.Peers[0].Id);
            Assert.AreEqual(node1.Id, _service3.Peers[0].Id);
        }

        private void AddNodes(params DiscoveryNode[] nodes)
        {
            _nodes.AddRange(nodes);
        }
    }
}