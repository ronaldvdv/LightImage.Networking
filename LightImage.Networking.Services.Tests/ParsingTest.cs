using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace LightImage.Networking.Services.Tests
{
    [TestClass]
    public class ParsingTest
    {
        private const ServiceClusterBehaviour C_BEHAVIOUR = ServiceClusterBehaviour.Global;
        private const string C_NAME = "My Service";
        private const string C_ROLE = "The Role";
        private readonly int[] C_PORTS = new[] { 17, 3, 8, 6 };

        [TestMethod]
        public void TestGuidEncoding()
        {
            var guid = Guid.NewGuid();
            var frame = guid.ToIdentityFrame();
            var other = frame.ToIdentityGuid();
            Assert.AreEqual(guid, other);
        }

        [TestMethod]
        public void TestServiceDataEncoding()
        {
            var data = new ServiceData(C_NAME, C_ROLE, C_PORTS, C_BEHAVIOUR);
            var encoded = data.ToByteArray();
            var other = ServiceData.Parse(encoded);
            Assert.AreEqual(C_NAME, other.Name);
            Assert.AreEqual(C_ROLE, other.Role);
            Assert.IsTrue(C_PORTS.SequenceEqual(other.Ports));
            Assert.AreEqual(C_BEHAVIOUR, other.ClusterBehaviour);
        }
    }
}