using LightImage.Networking.Discovery.Events;
using LightImage.Networking.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NetMQ;
using NetMQ.Sockets;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace LightImage.Networking.Discovery.Tests
{
    [TestClass]
    public partial class DiscoveryUnitTests
    {
        private NetMQBeacon _beacon;
        private DealerSocket _dealer;
        private Guid _id;
        private ILogger<DiscoveryNode> _logger;
        private ILoggerFactory _loggerFactory;
        private DiscoveryNode _node;
        private NetMQPoller _poller;
        private int _port;
        private RouterSocket _router;
        private Mock<IServiceManager> _services = new Mock<IServiceManager>();
        private ILogger<DiscoveryShim> _shimLogger;
        private TimeSpan C_TIMEOUT_SMALL = TimeSpan.FromSeconds(0.2);

        [TestCleanup]
        public void CleanUp()
        {
            if (_poller.IsRunning)
                _poller.Stop();
            _poller.Dispose();
            _node?.Dispose();
            _dealer?.Dispose();
            _beacon.Dispose();
            _router.Dispose();
            NetMQConfig.Cleanup();
            _loggerFactory.Dispose();
        }

        [TestInitialize]
        public void Initialize()
        {
            SynchronizationContext.SetSynchronizationContext(new TestSynchronizationContext());

            _router = new RouterSocket();
            _port = _router.BindRandomPort("tcp://*");

            _id = Guid.NewGuid();
            _beacon = new NetMQBeacon();
            _beacon.Configure(DiscoveryOptions.Default.BeaconPort);
            _beacon.Subscribe("");
            _beacon.Publish(new BeaconData(_id, _port, DiscoveryNode.C_NO_SESSION, 0).ToByteArray(), C_TIMEOUT_SMALL / 2);

            _loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            _logger = _loggerFactory.CreateLogger<DiscoveryNode>();
            _shimLogger = _loggerFactory.CreateLogger<DiscoveryShim>();
            _poller = new NetMQPoller { _beacon, _router };
        }

        [TestMethod]
        public void TestBeaconSent()
        {
            _node = new DiscoveryNode(_id, "Test", "test", _logger, _shimLogger, _services.Object, new DiscoveryOptions { BeaconInterval = C_TIMEOUT_SMALL / 2 });
            _poller.RunAsync();
            Assert.IsTrue(_beacon.TryReceive(C_TIMEOUT_SMALL, out var msg));
            var data = BeaconData.Parse(msg.Bytes);
            Assert.AreEqual(_node.Id, data.Id);
            Assert.AreEqual(DiscoveryNode.C_NO_SESSION, data.Session);
        }

        [TestMethod]
        [DataRow(DiscoveryNode.C_NO_SESSION, 1, 1)]
        [DataRow(DiscoveryNode.C_NO_SESSION, DiscoveryNode.C_NO_SESSION, DiscoveryNode.C_NO_SESSION)]
        [DataRow(1, DiscoveryNode.C_NO_SESSION, 1)]
        [DataRow(1, 1, 1)]
        [DataRow(1, 2, 1)]
        public void TestBeaconUpdatedWhenJoinReceived(int mySession, int initialSession, int expectedSession)
        {
            _node = new DiscoveryNode(Guid.NewGuid(), "Test", "test", _logger, _shimLogger, _services.Object, new DiscoveryOptions { BeaconInterval = C_TIMEOUT_SMALL / 2 });
            if (initialSession != DiscoveryNode.C_NO_SESSION)
                _node.Join(initialSession);
            _beacon.Publish(new BeaconData(_id, _port, mySession, 0).ToByteArray());
            _poller.RunAsync();

            Thread.Sleep(C_TIMEOUT_SMALL * 2);

            // Receive HELLO and connect
            NetMQMessage beaconMsg = null;
            Assert.IsTrue(_router.TryReceiveMultipartMessage(C_TIMEOUT_SMALL * 3, ref beaconMsg));
            var beacon = BeaconData.Parse(beaconMsg[2].Buffer);
            var host = beaconMsg[4].ConvertToString();

            _dealer = new DealerSocket($"tcp://{host}:{beacon.Port}");
            _dealer.Options.Identity = _id.ToIdentity();
            _dealer.SendFrame(DiscoveryMessages.C_MSG_JOIN);

            Thread.Sleep(C_TIMEOUT_SMALL * 2);
            BeaconData data = null;
            while (_beacon.TryReceive(TimeSpan.Zero, out var msg))
            {
                data = BeaconData.Parse(msg.Bytes);
                Debug.WriteLine($"Beacon received: {data.Session} from {data.Port}/{data.Id}");
            }
            Assert.AreEqual(expectedSession, data.Session);
        }

        [TestMethod]
        public void TestBeaconUpdatedWhenSessionChanged()
        {
            _node = new DiscoveryNode(Guid.NewGuid(), "Test", "test", _logger, _shimLogger, _services.Object, new DiscoveryOptions { BeaconInterval = C_TIMEOUT_SMALL / 2 });
            var session = 1;
            _node.Join(session);
            _poller.RunAsync();

            Thread.Sleep(C_TIMEOUT_SMALL * 2);
            BeaconData data = null;
            while (_beacon.TryReceive(TimeSpan.Zero, out var msg))
                data = BeaconData.Parse(msg.Bytes);
            Assert.AreEqual(_node.Id, data.Id);
            Assert.AreEqual(session, data.Session);
        }

        [TestMethod]
        public void TestBeaconUpdatedWhenStopping()
        {
            _node = new DiscoveryNode(Guid.NewGuid(), "Test", "test", _logger, _shimLogger, _services.Object, new DiscoveryOptions { BeaconInterval = C_TIMEOUT_SMALL / 2 });
            _poller.RunAsync();

            Thread.Sleep(C_TIMEOUT_SMALL);

            _node.Dispose();

            Thread.Sleep(C_TIMEOUT_SMALL);

            BeaconData data = null;
            while (_beacon.TryReceive(TimeSpan.Zero, out var msg))
                data = BeaconData.Parse(msg.Bytes);
            Assert.AreEqual(_node.Id, data.Id);
            Assert.AreEqual(0, data.Port);
        }

        [TestMethod]
        public void TestConnectsWhenBeaconReceived()
        {
            _node = new DiscoveryNode(Guid.NewGuid(), "Test", "test", _logger, _shimLogger, _services.Object, new DiscoveryOptions { BeaconInterval = C_TIMEOUT_SMALL / 2 });

            _poller.RunAsync();
            NetMQMessage msg = null;
            Assert.IsTrue(_router.TryReceiveMultipartMessage(C_TIMEOUT_SMALL * 3, ref msg));
            Assert.AreEqual(DiscoveryMessages.C_MSG_HELLO, msg[1].ConvertToString());
        }

        [TestMethod]
        public void TestHelloContainsServices()
        {
            var fakeService = new FakeService("TestService", "MyRole", new int[] { 3, 4 });
            var services = new Mock<IServiceManager>();
            services.Setup(sm => sm.GetDescriptors()).Returns(new[] { fakeService });
            _node = new DiscoveryNode(Guid.NewGuid(), "Test", "test", _logger, _shimLogger, services.Object, new DiscoveryOptions { BeaconInterval = C_TIMEOUT_SMALL / 2 });
            _poller.RunAsync();
            NetMQMessage msg = null;
            Assert.IsTrue(_router.TryReceiveMultipartMessage(C_TIMEOUT_SMALL * 3, ref msg));
            var service = ServiceData.Parse(msg.Last.Buffer);
            Assert.AreEqual("TestService", service.Name);
            Assert.AreEqual("MyRole", service.Role);
            Assert.IsTrue(service.Ports.SequenceEqual(new int[] { 3, 4 }));
        }

        [TestMethod]
        public void TestJoinSentWhenAddCalled()
        {
            _node = new DiscoveryNode(Guid.NewGuid(), "Test", "test", _logger, _shimLogger, _services.Object, new DiscoveryOptions { BeaconInterval = C_TIMEOUT_SMALL / 2 });
            _poller.RunAsync();
            _node.Join(1);
            Thread.Sleep(C_TIMEOUT_SMALL);
            _node.Add(_id);
            Thread.Sleep(C_TIMEOUT_SMALL);
            NetMQMessage msg = null;
            while (_router.TryReceiveMultipartMessage(TimeSpan.Zero, ref msg)) ;
            Assert.AreEqual(DiscoveryMessages.C_MSG_JOIN, msg[1].ConvertToString());
        }

        [TestMethod]
        public void TestNodeDisconnectsServiceOnPeerLeavesSession()
        {
            var fakeService = new FakeService("TestService", "MyRole", new int[] { 3, 4 });
            _services.Setup(sm => sm.GetDescriptors()).Returns(new[] { fakeService });
            var options = new DiscoveryOptions { BeaconInterval = C_TIMEOUT_SMALL / 2, TimerInterval = C_TIMEOUT_SMALL / 4, EvasiveThreshold = C_TIMEOUT_SMALL, LostThreshold = C_TIMEOUT_SMALL * 10 };
            int session = 1;
            _node = new DiscoveryNode(Guid.NewGuid(), "Test", "test", _logger, _shimLogger, _services.Object, options);
            _node.Join(session);
            _poller.RunAsync();

            PeerSessionChangedEventArgs args = null;
            var mre = new ManualResetEventSlim();
            _node.PeerSessionChanged += (_, e) => { args = e; mre.Set(); };

            Thread.Sleep(C_TIMEOUT_SMALL);
            _beacon.Publish(new BeaconData(_id, _port, session, 0).ToByteArray(), C_TIMEOUT_SMALL / 2);

            Assert.IsTrue(mre.Wait(C_TIMEOUT_SMALL));

            // Receive HELLO and connect
            NetMQMessage msg = null;
            Assert.IsTrue(_router.TryReceiveMultipartMessage(C_TIMEOUT_SMALL * 3, ref msg));
            var beacon = BeaconData.Parse(msg[2].Buffer);
            var host = msg[4].ConvertToString();

            _dealer = new DealerSocket($"tcp://{host}:{beacon.Port}");
            _dealer.Options.Identity = _id.ToIdentity();
            _dealer.SendHelloMessage(new BeaconData(_id, _port, session, 0).ToByteArray(), "", host, "", false, new[] { new ServiceData(fakeService.Name, fakeService.Role, fakeService.Ports) });

            Thread.Sleep(C_TIMEOUT_SMALL);

            mre.Reset();
            _beacon.Publish(new BeaconData(_id, _port, session + 1, session).ToByteArray(), C_TIMEOUT_SMALL / 2);

            Assert.IsTrue(mre.Wait(C_TIMEOUT_SMALL));
            _services.Verify(sm => sm.Disconnect(_id, fakeService.Name));
        }

        [TestMethod]
        public void TestNodeDisconnectsWhenBeaconStopsGracefully()
        {
            var options = new DiscoveryOptions { BeaconInterval = C_TIMEOUT_SMALL / 2, TimerInterval = C_TIMEOUT_SMALL / 2, EvasiveThreshold = C_TIMEOUT_SMALL };
            _node = new DiscoveryNode(Guid.NewGuid(), "Test", "test", _logger, _shimLogger, _services.Object, options);
            _poller.RunAsync();
            NetMQMessage msg = null;

            Thread.Sleep(C_TIMEOUT_SMALL);
            _beacon.Publish(new BeaconData(_id, 0, DiscoveryNode.C_NO_SESSION, 0).ToByteArray());
            Thread.Sleep(C_TIMEOUT_SMALL / 2);
            _poller.Remove(_beacon);
            _beacon.Silence();

            Thread.Sleep(C_TIMEOUT_SMALL * 2);

            while (_router.TryReceiveMultipartMessage(TimeSpan.Zero, ref msg))
            {
                var cmd = msg[1].ConvertToString();
                Assert.AreNotEqual(DiscoveryMessages.C_MSG_PING, cmd);
            }
        }

        [TestMethod]
        public void TestNodeSendsPingWhenBeaconStops()
        {
            var options = new DiscoveryOptions { BeaconInterval = C_TIMEOUT_SMALL / 2, TimerInterval = C_TIMEOUT_SMALL / 2, EvasiveThreshold = C_TIMEOUT_SMALL };
            _node = new DiscoveryNode(Guid.NewGuid(), "Test", "test", _logger, _shimLogger, _services.Object, options);
            _poller.RunAsync();
            NetMQMessage msg = null;

            Thread.Sleep(C_TIMEOUT_SMALL);
            _poller.Remove(_beacon);
            _beacon.Silence();

            Thread.Sleep(C_TIMEOUT_SMALL * 2);

            while (_router.TryReceiveMultipartMessage(TimeSpan.Zero, ref msg)) ;
            var cmd = msg[1].ConvertToString();
            Assert.AreEqual(DiscoveryMessages.C_MSG_PING, cmd);
        }

        [TestMethod]
        public void TestPeerServiceIsRegistered()
        {
            var fakeService = new FakeService("TestService", "MyRole", new int[] { 3, 4 });
            var services = new Mock<IServiceManager>();
            services.Setup(sm => sm.GetDescriptors()).Returns(new[] { fakeService });
            _node = new DiscoveryNode(new Guid(), "Test", "test", _logger, _shimLogger, services.Object, new DiscoveryOptions { BeaconInterval = C_TIMEOUT_SMALL / 2 });
            var session = 1;
            _node.Join(session);
            _poller.RunAsync();

            // Receive HELLO and connect
            NetMQMessage msg = null;
            Assert.IsTrue(_router.TryReceiveMultipartMessage(C_TIMEOUT_SMALL * 3, ref msg));
            var beacon = BeaconData.Parse(msg[2].Buffer);
            var host = msg[4].ConvertToString();

            _dealer = new DealerSocket($"tcp://{host}:{beacon.Port}");
            _dealer.Options.Identity = _id.ToIdentity();

            // Setup event
            var e = new ManualResetEventSlim();
            _node.ServiceDiscovered += (sender, args) => e.Set();

            // Say HELLO with one service, in the same session
            _dealer.SendHelloMessage(new BeaconData(_id, _port, session, 0).ToByteArray(), "", host, "", true, new[] { new ServiceData(fakeService.Name, fakeService.Role, fakeService.Ports) });

            // Now see if the fake service is notified
            Assert.IsTrue(e.Wait(C_TIMEOUT_SMALL));
            services.Verify(sm => sm.Connect(_id, fakeService.Name, _node.Host, fakeService.Ports, fakeService.Role));
        }

        #region Events

        [TestMethod]
        public void TestHeartbeatMarksAlive()
        {
            var options = new DiscoveryOptions { BeaconInterval = C_TIMEOUT_SMALL / 2, TimerInterval = C_TIMEOUT_SMALL / 4, EvasiveThreshold = C_TIMEOUT_SMALL };
            _node = new DiscoveryNode(Guid.NewGuid(), "Test", "test", _logger, _shimLogger, _services.Object, options);
            _poller.RunAsync();

            PeerEventArgs args = null;
            var mreEvasive = new ManualResetEventSlim();
            _node.PeerEvasive += (_, e) => { args = e; mreEvasive.Set(); };

            Thread.Sleep(C_TIMEOUT_SMALL * 2);
            _poller.Remove(_beacon);
            _beacon.Silence();

            Assert.IsTrue(mreEvasive.Wait(C_TIMEOUT_SMALL * 2));
            Assert.AreEqual(_id, args.Id);

            var mreReturned = new ManualResetEventSlim();
            _node.PeerReturned += (_, e) => { args = e; mreReturned.Set(); };
            args = null;
            _services.Raise(sm => sm.PeerHeartbeat += null, new ServicePeerHeartbeatEventArgs(string.Empty, _id));
            Assert.IsTrue(mreReturned.Wait(C_TIMEOUT_SMALL * 2));
            Assert.AreEqual(_id, args.Id);
        }

        [TestMethod]
        public void TestPeerDiscoveredRaised()
        {
            _node = new DiscoveryNode(Guid.NewGuid(), "Test", "test", _logger, _shimLogger, _services.Object, new DiscoveryOptions { BeaconInterval = C_TIMEOUT_SMALL / 2 });
            var mre = new ManualResetEventSlim();
            PeerDiscoveredEventArgs args = null;
            _node.PeerDiscovered += (_, e) => { args = e; mre.Set(); };
            _poller.RunAsync();

            Assert.IsTrue(mre.Wait(C_TIMEOUT_SMALL));
            Assert.AreEqual(_id, args.Id);
        }

        [TestMethod]
        public void TestPeerEvasiveRaised()
        {
            var options = new DiscoveryOptions { BeaconInterval = C_TIMEOUT_SMALL / 2, TimerInterval = C_TIMEOUT_SMALL / 4, EvasiveThreshold = C_TIMEOUT_SMALL };
            _node = new DiscoveryNode(Guid.NewGuid(), "Test", "test", _logger, _shimLogger, _services.Object, options);
            _poller.RunAsync();

            PeerEventArgs args = null;
            var mre = new ManualResetEventSlim();
            _node.PeerEvasive += (_, e) => { args = e; mre.Set(); };

            Thread.Sleep(C_TIMEOUT_SMALL * 2);
            _poller.Remove(_beacon);
            _beacon.Silence();

            Assert.IsTrue(mre.Wait(C_TIMEOUT_SMALL * 2));
            Assert.AreEqual(_id, args.Id);
        }

        [TestMethod]
        public void TestPeerLostRaised()
        {
            var fakeService = new FakeService("TestService", "MyRole", new int[] { 3, 4 });
            _services.Setup(sm => sm.GetDescriptors()).Returns(new[] { fakeService });
            var options = new DiscoveryOptions { BeaconInterval = C_TIMEOUT_SMALL / 2, TimerInterval = C_TIMEOUT_SMALL / 4, EvasiveThreshold = C_TIMEOUT_SMALL, LostThreshold = C_TIMEOUT_SMALL * 3 };
            _node = new DiscoveryNode(Guid.NewGuid(), "Test", "test", _logger, _shimLogger, _services.Object, options);
            _poller.RunAsync();

            PeerEventArgs args = null;
            var mre = new ManualResetEventSlim();
            _node.PeerLost += (_, e) => { args = e; mre.Set(); };

            Thread.Sleep(C_TIMEOUT_SMALL);
            _poller.Remove(_beacon);
            _beacon.Silence();

            Assert.IsFalse(mre.Wait(C_TIMEOUT_SMALL * 2));
            Assert.IsTrue(mre.Wait(C_TIMEOUT_SMALL * 2));
            Assert.AreEqual(_id, args.Id);
            _services.Verify(sm => sm.Remove(_id));
        }

        [TestMethod]
        public void TestPeerReturnedRaised()
        {
            var options = new DiscoveryOptions { BeaconInterval = C_TIMEOUT_SMALL / 2, TimerInterval = C_TIMEOUT_SMALL / 4, EvasiveThreshold = C_TIMEOUT_SMALL, LostThreshold = C_TIMEOUT_SMALL * 10 };
            _node = new DiscoveryNode(Guid.NewGuid(), "Test", "test", _logger, _shimLogger, _services.Object, options);
            _poller.RunAsync();

            PeerEventArgs args = null;
            var mre = new ManualResetEventSlim();
            _node.PeerReturned += (_, e) => { args = e; mre.Set(); };

            Thread.Sleep(C_TIMEOUT_SMALL);
            _poller.Remove(_beacon);
            _beacon.Silence();

            Thread.Sleep(C_TIMEOUT_SMALL * 2);
            _poller.Add(_beacon);
            _beacon.Publish(new BeaconData(_id, _port, DiscoveryNode.C_NO_SESSION, 0).ToByteArray(), C_TIMEOUT_SMALL / 2);

            Assert.IsTrue(mre.Wait(C_TIMEOUT_SMALL));
            Assert.AreEqual(_id, args.Id);
        }

        [TestMethod]
        public void TestPeerSessionChangedRaised()
        {
            var options = new DiscoveryOptions { BeaconInterval = C_TIMEOUT_SMALL / 2, TimerInterval = C_TIMEOUT_SMALL / 4, EvasiveThreshold = C_TIMEOUT_SMALL, LostThreshold = C_TIMEOUT_SMALL * 10 };
            _node = new DiscoveryNode(Guid.NewGuid(), "Test", "test", _logger, _shimLogger, _services.Object, options);
            _poller.RunAsync();

            PeerSessionChangedEventArgs args = null;
            var mre = new ManualResetEventSlim();
            _node.PeerSessionChanged += (_, e) => { args = e; mre.Set(); };

            Thread.Sleep(C_TIMEOUT_SMALL);
            _beacon.Publish(new BeaconData(_id, _port, 3, 0).ToByteArray(), C_TIMEOUT_SMALL / 2);

            Assert.IsTrue(mre.Wait(C_TIMEOUT_SMALL));
            Assert.AreEqual(_id, args.Id);
            Assert.AreEqual(3, args.Session);
        }

        [TestMethod]
        public void TestSessionChangedRaised()
        {
            var options = new DiscoveryOptions { BeaconInterval = C_TIMEOUT_SMALL / 2, TimerInterval = C_TIMEOUT_SMALL / 4, EvasiveThreshold = C_TIMEOUT_SMALL, LostThreshold = C_TIMEOUT_SMALL * 10 };
            _node = new DiscoveryNode(Guid.NewGuid(), "Test", "test", _logger, _shimLogger, _services.Object, options);
            _poller.RunAsync();

            SessionChangedEventArgs args = null;
            var mre = new ManualResetEventSlim();
            _node.SessionChanged += (_, e) => { args = e; mre.Set(); };

            _node.Join(3);
            Assert.IsTrue(mre.Wait(C_TIMEOUT_SMALL));
            Assert.AreEqual(3, args.Session);
        }

        #endregion Events
    }
}