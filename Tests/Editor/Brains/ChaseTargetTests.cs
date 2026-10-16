using NUnit.Framework;
using RogueLikeEngine.Systems.Brains;
using RogueLikeEngine.Systems.Brains.Nodes;
using RogueLikeEngine.Systems.Entities;
using UnityEngine;

namespace RogueLikeEngine.Tests
{
    public class ChaseTargetTests
    {
        private TestObjects m_objects;
        private Entity m_owner;
        private Entity m_target;
        private RecordingMovement m_movement;
        private BrainContext m_context;
        private ChaseTarget m_chase;

        [SetUp]
        public void SetUp()
        {
            m_objects = new TestObjects();
            m_owner = m_objects.CreateComponent<Entity>();
            m_owner.transform.position = new Vector3(10, 0, 20);
            m_owner.gameObject.AddComponent<Rigidbody>();
            m_movement = m_owner.gameObject.AddComponent<RecordingMovement>();
            TestReflection.SetField(m_owner, "m_movement", m_movement);
            TestReflection.Invoke(m_movement, "Awake");
            m_target = m_objects.CreateComponent<Entity>();
            m_target.transform.position = new Vector3(30, 0, 40);
            m_context = new BrainContext(m_owner, System.Array.Empty<BrainVariable>());
            m_chase = new ChaseTarget
            {
                Target = new BrainValue<Entity> { Constant = m_target },
                StopDistance = new BrainValue<float> { Constant = 2 }
            };
        }

        [TearDown]
        public void TearDown() => m_objects.Dispose();

        [Test]
        public void Chase_PassesTargetPositionAndUsesOwnerPositionWhenInterrupted()
        {
            Assert.AreEqual(BrainStatus.Running, m_chase.Tick(m_context));
            Assert.AreEqual(m_target.transform.position, m_movement.LastInput);

            m_target.transform.position = new Vector3(40, 0, 50);
            m_chase.Tick(m_context);
            Assert.AreEqual(m_target.transform.position, m_movement.LastInput);

            m_chase.Reset(m_context);
            Assert.AreEqual(m_owner.transform.position, m_movement.LastInput);
        }

        [Test]
        public void Chase_WithinStoppingDistanceUsesOwnerPosition()
        {
            m_target.transform.position = m_owner.transform.position + Vector3.right;

            Assert.AreEqual(BrainStatus.Running, m_chase.Tick(m_context));
            Assert.AreEqual(m_owner.transform.position, m_movement.LastInput);
        }
    }
}
