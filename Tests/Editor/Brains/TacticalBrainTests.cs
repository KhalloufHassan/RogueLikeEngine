using NUnit.Framework;
using RogueLikeEngine.Systems.Brains;
using RogueLikeEngine.Systems.Brains.Nodes;
using RogueLikeEngine.Systems.Entities;
using UnityEngine;

namespace RogueLikeEngine.Tests
{
    public class TacticalBrainTests
    {
        private BrainContext m_context;

        [SetUp]
        public void SetUp()
        {
            m_context = new BrainContext(null, new BrainVariable[] { new BrainFloat { Name = "Aggression" } });
            typeof(BrainContext).GetProperty(nameof(BrainContext.DeltaTime)).SetValue(m_context, .1f);
        }

        [Test]
        public void ChanceUsesPerEntityAggressionAndKeepsItsDecisionUntilReset()
        {
            Chance chance = new() { Probability = new BrainValue<float> { Variable = "Aggression" } };
            Assert.AreEqual(BrainStatus.Failure, chance.Tick(m_context));
            m_context.Set("Aggression", 1f);
            Assert.AreEqual(BrainStatus.Failure, chance.Tick(m_context));
            chance.Reset(m_context);
            Assert.AreEqual(BrainStatus.Success, chance.Tick(m_context));
        }

        [Test]
        public void TimedActionStopsRunningChildrenWhenDurationExpires()
        {
            Probe child = new();
            TimedAction action = new() { Child = child, Duration = new BrainValue<float> { Constant = .15f } };
            Assert.AreEqual(BrainStatus.Running, action.Tick(m_context));
            Assert.AreEqual(BrainStatus.Success, action.Tick(m_context));
            Assert.AreEqual(1, child.Stops);
            action.Reset(m_context);
            Assert.AreEqual(1, child.Stops);
            Assert.AreEqual(BrainStatus.Running, action.Tick(m_context));
            Assert.AreEqual(2, child.Starts);
        }

        [Test]
        public void InterruptingTimedActionStopsItsChild()
        {
            Probe child = new();
            TimedAction action = new() { Child = child, Duration = new BrainValue<float> { Constant = 3 } };
            action.Tick(m_context);
            action.Reset(m_context);
            Assert.AreEqual(1, child.Stops);
        }

        [Test]
        public void TimedActionPreservesChildFailure()
        {
            Probe child = new() { Result = BrainStatus.Failure };
            TimedAction action = new() { Child = child, Duration = new BrainValue<float> { Constant = 3 } };
            Assert.AreEqual(BrainStatus.Failure, action.Tick(m_context));
            Assert.AreEqual(1, child.Stops);
        }

        [Test]
        public void ZeroDurationDoesNotStartFiring()
        {
            Probe child = new();
            TimedAction action = new() { Child = child };
            Assert.AreEqual(BrainStatus.Success, action.Tick(m_context));
            Assert.AreEqual(0, child.Starts);
        }

        [Test]
        public void MoveToPositionStopsOnTimeoutAndUsesWorldDestinations()
        {
            using TestObjects objects = new();
            Entity owner = objects.CreateComponent<Entity>();
            owner.gameObject.AddComponent<Rigidbody>();
            RecordingMovement movement = owner.gameObject.AddComponent<RecordingMovement>();
            TestReflection.SetField(owner, "m_movement", movement);
            TestReflection.Invoke(movement, "Awake");
            BrainContext context = new(owner, System.Array.Empty<BrainVariable>());
            typeof(BrainContext).GetProperty(nameof(BrainContext.DeltaTime)).SetValue(context, .1f);
            Vector3 destination = new(10, 0, 20);
            MoveToPosition move = new()
            {
                Destination = new BrainValue<Vector3> { Constant = destination },
                Timeout = new BrainValue<float> { Constant = .15f }
            };
            Assert.AreEqual(BrainStatus.Running, move.Tick(context));
            Assert.AreEqual(destination, movement.LastInput);
            Assert.AreEqual(BrainStatus.Failure, move.Tick(context));
            Assert.AreEqual(owner.transform.position, movement.LastInput);
        }

        [Test]
        public void FireRequestsExposureButOnlyShootsOnceClearAndCancelsAfterBurst()
        {
            using TestObjects objects = new();
            Entity owner = objects.CreateComponent<Entity>();
            owner.gameObject.AddComponent<Rigidbody>();
            RecordingMovement movement = owner.gameObject.AddComponent<RecordingMovement>();
            TestReflection.Invoke(movement, "Awake");
            TestReflection.SetField(owner, "m_movement", movement);
            RecordingWeaponsSystem weapons = owner.gameObject.AddComponent<RecordingWeaponsSystem>();
            TestReflection.SetField(owner, "m_weaponsSystem", weapons);
            TestReflection.SetField(weapons, "m_entity", owner);
            Entity target = objects.CreateComponent<Entity>();
            target.transform.position = Vector3.forward * 10;
            movement.InCover = true;
            BrainContext context = new(owner, System.Array.Empty<BrainVariable>());
            typeof(BrainContext).GetProperty(nameof(BrainContext.DeltaTime)).SetValue(context, .1f);
            TimedAction burst = new()
            {
                Duration = new BrainValue<float> { Constant = .25f },
                Child = new FireAtTarget { Target = new BrainValue<Entity> { Constant = target } }
            };
            burst.Tick(context);
            Assert.IsTrue(weapons.IsFireHeld);
            Assert.AreEqual(0, weapons.Shots);
            movement.Exposed = true;
            burst.Tick(context);
            Assert.AreEqual(1, weapons.Shots);
            movement.Exposed = false;
            Assert.AreEqual(BrainStatus.Success, burst.Tick(context));
            Assert.AreEqual(1, weapons.Shots);
            Assert.IsFalse(weapons.IsFireHeld);
        }
    }
}
