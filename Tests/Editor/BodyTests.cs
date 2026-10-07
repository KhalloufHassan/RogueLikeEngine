using NUnit.Framework;
using RogueLikeEngine.Systems.Movements;
using UnityEngine;

namespace RogueLikeEngine.Tests
{
    public class BodyTests
    {
        private TestObjects m_objects;

        [SetUp]
        public void SetUp() => m_objects = new TestObjects();

        [TearDown]
        public void TearDown() => m_objects.Dispose();

        private Body2D Create2D() => new(m_objects.CreateComponent<Rigidbody2D>());
        private Body3D Create3D() => new(m_objects.CreateComponent<Rigidbody>());

        private static void AssertClose(Vector3 expected, Vector3 actual) =>
            Assert.That(Vector3.Distance(expected, actual), Is.LessThan(1e-4f), $"Expected {expected} but was {actual}");

        [Test]
        public void Factory_PicksTheBodyMatchingTheRigidbody()
        {
            Assert.IsInstanceOf<Body2D>(BodyFactory.Find(m_objects.CreateComponent<Rigidbody2D>().gameObject));
            Assert.IsInstanceOf<Body3D>(BodyFactory.Find(m_objects.CreateComponent<Rigidbody>().gameObject));
            Assert.IsNull(BodyFactory.Find(m_objects.Track(new GameObject("Empty"))));
        }

        [Test]
        public void PlanarInput_MapsToTheMovementPlane()
        {
            AssertClose(new Vector3(1, 2, 0), Create2D().ToWorld(new Vector2(1, 2)));
            AssertClose(new Vector3(1, 0, 2), Create3D().ToWorld(new Vector2(1, 2)));
        }

        [Test]
        public void Flatten_RemovesTheOffPlaneAxis()
        {
            AssertClose(new Vector3(1, 2, 0), Create2D().Flatten(new Vector3(1, 2, 3)));
            AssertClose(new Vector3(1, 0, 3), Create3D().Flatten(new Vector3(1, 2, 3)));
        }

        [Test]
        public void SetPlanarVelocity_In3D_KeepsTheVerticalVelocity()
        {
            Rigidbody rigidbody = m_objects.CreateComponent<Rigidbody>();
            rigidbody.linearVelocity = new Vector3(0, -4, 0);

            new Body3D(rigidbody).SetPlanarVelocity(new Vector3(2, 9, 3));

            AssertClose(new Vector3(2, -4, 3), rigidbody.linearVelocity);
        }

        [Test]
        public void SetVelocity_In3D_SetsTheFullVelocity()
        {
            Rigidbody rigidbody = m_objects.CreateComponent<Rigidbody>();

            new Body3D(rigidbody).SetVelocity(new Vector3(2, 9, 3));

            AssertClose(new Vector3(2, 9, 3), rigidbody.linearVelocity);
        }

        [Test]
        public void FacingRotation_2DSpritesFaceUp_3DModelsFaceForward()
        {
            Vector3 direction2D = new Vector3(1, 1, 0).normalized;
            AssertClose(direction2D, Create2D().FacingRotation(direction2D) * Vector3.up);

            Vector3 direction3D = new Vector3(1, 0, 1).normalized;
            AssertClose(direction3D, Create3D().FacingRotation(direction3D) * Vector3.forward);
        }
    }
}
