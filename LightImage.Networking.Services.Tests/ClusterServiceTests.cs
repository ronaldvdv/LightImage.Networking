using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace LightImage.Networking.Services.Tests
{
    [TestClass]
    public class ClusterServiceTests
    {
        private const string C_HOST = "localhost";
        private const string C_MY_NAME = "My Service";
        private const string C_MY_ROLE = "My Role";
        private readonly Guid C_MY_ID = Guid.NewGuid();
        private readonly TimeSpan C_TIMEOUT = TimeSpan.FromMilliseconds(500);
        private RouterSocket _router;
        private int _routerPort;
        private TestService _service;

        [TestInitialize]
        public void Initialize()
        {
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            SynchronizationContext.SetSynchronizationContext(new TestSynchronizationContext());

            _service = new TestService(C_MY_ID, C_HOST, C_MY_NAME, C_MY_ROLE, loggerFactory.CreateLogger<TestService>(), loggerFactory.CreateLogger<TestShim>());
            _service.Start();

            _router = new RouterSocket();
            _routerPort = _router.BindRandomPort($"tcp://{C_HOST}");
        }

        [TestMethod]
        public void TestAddConnectsOutward()
        {
            var C_OTHER_ID = Guid.NewGuid();
            const string C_OTHER_ROLE = "Other Role";

            _service.Add(C_OTHER_ID, C_HOST, C_OTHER_ROLE, new[] { _routerPort });
            NetMQMessage msg = null;
            Assert.IsTrue(_router.TryReceiveMultipartMessage(C_TIMEOUT, ref msg));

            Assert.AreEqual(C_MY_ID, msg[0].ToIdentityGuid());
            Assert.AreEqual(ClusterMessages.C_MSG_HELLO, msg[1].ConvertToString());

            ClusterMessages.ParseHello(msg, out var endpoint, out var role);
            Assert.AreEqual($"tcp://{C_HOST}:{_service.Ports[0]}", endpoint);
            Assert.AreEqual(C_MY_ROLE, role);
        }

        [TestMethod]
        public void TestInitialSetup()
        {
            Assert.AreEqual(C_HOST, _service.Host);
            Assert.AreEqual(C_MY_ID, _service.Id);
            Assert.AreEqual(C_MY_ROLE, _service.Role);
            Assert.AreEqual(1, _service.Ports.Length);
        }

        [TestMethod]
        public void TestShimSendsToActor()
        {
            var C_OTHER_ID = Guid.NewGuid();
            const string C_OTHER_ROLE = "Other Role";

            _service.Add(C_OTHER_ID, C_HOST, C_OTHER_ROLE, new[] { _routerPort });
            NetMQMessage msg = null;
            _router.TryReceiveMultipartMessage(C_TIMEOUT, ref msg); // HELLO
            ClusterMessages.ParseHello(msg, out var endpoint, out var _);

            var dealer = new DealerSocket(endpoint);
            dealer.Options.Identity = C_OTHER_ID.ToIdentity();
            ClusterMessages.SendHello(dealer, $"tcp://{C_HOST}:{_routerPort}", C_OTHER_ROLE);

            Guid heartbeat = Guid.Empty;
            _service.PeerHeartbeat += (sender, args) => heartbeat = args.PeerId;
            dealer.SendMoreFrame(TestShim.C_COMMAND).SendFrame(17);

            Assert.IsTrue(_service.ActorMessageReceivedEvent.Wait(C_TIMEOUT));
            Assert.AreEqual(C_OTHER_ID, heartbeat);
        }

        [TestMethod]
        public void TestUnknownHelloGetsReply()
        {
            var C_OTHER_ID = Guid.NewGuid();
            const string C_OTHER_ROLE = "Other Role";
            var dealer = new DealerSocket($"tcp://{_service.Host}:{_service.Ports[0]}");
            dealer.Options.Identity = C_OTHER_ID.ToIdentity();
            ClusterMessages.SendHello(dealer, $"tcp://{C_HOST}:{_routerPort}", C_OTHER_ROLE);
            NetMQMessage msg = null;
            Assert.IsTrue(_router.TryReceiveMultipartMessage(C_TIMEOUT, ref msg));
            Assert.AreEqual(ClusterMessages.C_MSG_HELLO, msg[1].ConvertToString());
            Guid receivedId = msg[0].ToIdentityGuid();
            Assert.AreEqual(C_MY_ID, receivedId);
        }

        private class TestPeer : ClusterShimPeer
        {
            public TestPeer(Guid id, string endpoint, Guid me, string role) : base(id, endpoint, me, role)
            {
            }
        }

        private class TestService : ClusterService<TestShim, TestPeer>
        {
            private readonly ILogger<TestShim> _shimLogger;

            public TestService(Guid id, string host, string name, string role, ILogger<TestService> serviceLogger, ILogger<TestShim> shimLogger) : base(id, host, name, role, serviceLogger)
            {
                _shimLogger = shimLogger;
            }

            public ManualResetEventSlim ActorMessageReceivedEvent { get; } = new ManualResetEventSlim();
            public NetMQFrame[] Message { get; private set; }

            protected override TestShim CreateShim()
            {
                return new TestShim(Id, Host, Role, _shimLogger);
            }

            protected override void HandleActorEvent(string cmd, NetMQMessage msg)
            {
                switch (cmd)
                {
                    case TestShim.C_COMMAND:
                        Message = msg.Select(f => f.Duplicate()).ToArray();
                        ActorMessageReceivedEvent.Set();
                        break;

                    default:
                        base.HandleActorEvent(cmd, msg);
                        break;
                }
            }
        }

        private class TestShim : ClusterShim<TestPeer>
        {
            public const string C_COMMAND = "MY-CMD";

            public TestShim(Guid id, string host, string role, ILogger<TestShim> logger) : base(id, host, role, logger)
            {
            }

            protected override TestPeer CreatePeer(Guid id, string endpoint, string role)
            {
                return new TestPeer(id, endpoint, Id, role);
            }

            protected override void HandleRouterMessage(string cmd, NetMQMessage msg, Guid identity)
            {
                if (cmd == C_COMMAND)
                {
                    int value = msg[1].ConvertToInt32();
                    Shim.Send(socket =>
                    {
                        socket.SendMoreFrame(C_COMMAND);
                        socket.SendFrame(value);
                    });
                    return;
                }
                base.HandleRouterMessage(cmd, msg, identity);
            }
        }
    }
}