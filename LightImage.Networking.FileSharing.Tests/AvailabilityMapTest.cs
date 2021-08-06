using LightImage.Networking.FileSharing.Policies;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace LightImage.Networking.FileSharing.Tests
{
    [TestClass]
    public class AvailabilityMapTest
    {
        private readonly RetryPolicyConfig _policy = RetryPolicyConfig.Exponential(int.MaxValue, MS(20), 2, MS(80));

        [TestMethod]
        public void TestAvailabilityExpiresOnRemove()
        {
            var map = new AvailabilityMap(_policy);
            var id = Guid.NewGuid();
            map.Set(id, false);
            map.Remove(id);
            Assert.AreEqual(Availability.AvailabilityExpired, map.Get(id));
        }

        [TestMethod]
        public void TestAvailabilityExpiryIntervalIncreases()
        {
            var map = new AvailabilityMap(_policy);
            var id = Guid.NewGuid();
            map.Set(id, false);
            map.Set(id, false);
            Assert.AreEqual(Availability.Unavailable, map.Get(id));
            Task.Delay(MS(20)).Wait();
            Assert.AreEqual(Availability.AvailabilityExpired, map.Get(id), "Unavailability should expire");
        }

        [TestMethod]
        public void TestGetUndefinedAvailability()
        {
            var map = new AvailabilityMap(_policy);
            Assert.AreEqual(Availability.AvailabilityExpired, map.Get(Guid.NewGuid()));
        }

        [TestMethod]
        public void TestPolicy()
        {
            Assert.AreEqual(MS(20), _policy.GetInterval(0));
            Assert.AreEqual(MS(40), _policy.GetInterval(1));
            Assert.AreEqual(MS(80), _policy.GetInterval(2));
            Assert.AreEqual(MS(80), _policy.GetInterval(3));
        }

        [TestMethod]
        public void TestSetAvailable()
        {
            var map = new AvailabilityMap(_policy);
            var id = Guid.NewGuid();
            map.Set(id, true);
            Assert.AreEqual(Availability.Available, map.Get(id));
            Task.Delay(MS(15)).Wait();
            Assert.AreEqual(Availability.Available, map.Get(id), "Unavailability should not expire");
        }

        [TestMethod]
        public void TestSetAvailableAfterUnavailability()
        {
            var map = new AvailabilityMap(_policy);
            var id = Guid.NewGuid();
            map.Set(id, false);
            map.Set(id, true);
            Task.Delay(MS(15)).Wait();
            Assert.AreEqual(Availability.Available, map.Get(id), "Available state should not expire");
        }

        [TestMethod]
        public void TestSetUnavailableOnce()
        {
            var map = new AvailabilityMap(_policy);
            var id = Guid.NewGuid();
            map.Set(id, false);
            Assert.AreEqual(Availability.Unavailable, map.Get(id));
            Task.Delay(MS(15)).Wait();
            Assert.AreEqual(Availability.AvailabilityExpired, map.Get(id), "Unavailability should expire");
        }

        [TestMethod]
        public void TestUnavailabilityExpiryResetsAfterAvailability()
        {
            var map = new AvailabilityMap(_policy);
            var id = Guid.NewGuid();
            map.Set(id, false);
            map.Set(id, false);
            map.Set(id, false);
            map.Set(id, true);
            map.Set(id, false);
            Task.Delay(MS(30)).Wait();
            Assert.AreEqual(Availability.AvailabilityExpired, map.Get(id), "Unavailable expiry should reset");
        }

        private static TimeSpan MS(int ms) => TimeSpan.FromMilliseconds(ms);
    }
}