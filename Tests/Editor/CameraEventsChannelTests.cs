using NUnit.Framework;
using RogueLikeEngine.Cameras;
using UnityEngine;

namespace RogueLikeEngine.Tests
{
    public class CameraEventsChannelTests
    {
        [Test]
        public void Shake_ReachesEverySubscriberWithTheImpulse()
        {
            CameraEventsChannel channel = ScriptableObject.CreateInstance<CameraEventsChannel>();
            Vector3 received = Vector3.zero;
            int calls = 0;
            channel.OnShake += impulse =>
            {
                received = impulse;
                calls++;
            };

            channel.Shake(Vector3.forward * .4f);

            Assert.AreEqual(1, calls);
            Assert.AreEqual(Vector3.forward * .4f, received);
            Object.DestroyImmediate(channel);
        }
    }
}
