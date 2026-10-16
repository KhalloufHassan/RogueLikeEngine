using NUnit.Framework;
using RogueLikeEngine.Systems.Brains;
using RogueLikeEngine.Systems.Brains.Nodes;
using RogueLikeEngine.Systems.Entities;
using UnityEngine;

namespace RogueLikeEngine.Tests
{
    public class BrainTests
    {
        private TestObjects m_objects;
        private BrainGraph m_graph;

        [SetUp]
        public void SetUp()
        {
            m_objects = new TestObjects();
            m_graph = m_objects.CreateAsset<BrainGraph>();
            m_graph.SetDefinition(new CountTicks(), new BrainVariable[]
            {
                new BrainInt { Name = "Count" },
                new BrainEntity { Name = "Target" },
                new BrainFloat { Name = "Radius", Value = 15 }
            });
        }

        [TearDown]
        public void TearDown() => m_objects.Dispose();

        [Test]
        public void SharedGraph_HasIndependentVariablesAndActionState()
        {
            Brain first = CreateBrain();
            Brain second = CreateBrain();
            TestReflection.Invoke(first, "Update");
            TestReflection.Invoke(first, "Update");
            TestReflection.Invoke(second, "Update");

            Assert.AreEqual(2, first.Context.Get<int>("Count"));
            Assert.AreEqual(1, second.Context.Get<int>("Count"));
            Assert.AreEqual(0, ((BrainInt)m_graph.Variables[0]).Value);
            Assert.AreEqual(0, ((CountTicks)m_graph.Root).Ticks);
        }

        [Test]
        public void Overrides_AllowDifferentValuesPerBrainWithoutChangingGraphDefaults()
        {
            Brain first = CreateBrain(new BrainFloat { Name = "Radius", Value = 5 });
            Brain second = CreateBrain(new BrainFloat { Name = "Radius", Value = 30 });
            Brain defaults = CreateBrain();

            Assert.AreEqual(5, first.Context.Get<float>("Radius"));
            Assert.AreEqual(30, second.Context.Get<float>("Radius"));
            Assert.AreEqual(15, defaults.Context.Get<float>("Radius"));
            Assert.AreEqual(15, ((BrainFloat)m_graph.Variables[2]).Value);
        }

        [Test]
        public void RuntimeChanges_DoNotAlterSerializedOverridesAndSurviveRestart()
        {
            BrainFloat radius = new() { Name = "Radius", Value = 5 };
            Brain brain = CreateBrain(radius);
            brain.Context.Set("Radius", 40f);
            brain.Restart();

            Assert.AreEqual(40, brain.Context.Get<float>("Radius"));
            Assert.AreEqual(5, radius.Value);
            Assert.AreEqual(15, ((BrainFloat)m_graph.Variables[2]).Value);
        }

        [Test]
        public void Overrides_CanSupplySceneReferences()
        {
            Entity target = m_objects.CreateComponent<Entity>();
            Brain brain = CreateBrain(new BrainEntity { Name = "Target", Value = target });

            Assert.AreSame(target, brain.Context.Get<Entity>("Target"));
            Assert.IsNull(((BrainEntity)m_graph.Variables[1]).Value);
        }

        [Test]
        public void StaleOverrides_AfterGraphChangesDoNotReplaceValidDefaults()
        {
            Brain brain = CreateBrain(
                new BrainInt { Name = "Radius", Value = 5 },
                new BrainFloat { Name = "Removed", Value = 30 });

            Assert.AreEqual(15, brain.Context.Get<float>("Radius"));
            Assert.AreEqual(0, brain.Context.Get<int>("Count"));
        }

        [Test]
        public void Locks_RestartExecutionWhenAllOwnersReleaseWithoutClearingTarget()
        {
            Brain brain = CreateBrain();
            Entity target = m_objects.CreateComponent<Entity>();
            brain.Context.Set("Target", target);
            TestReflection.Invoke(brain, "Update");
            TestReflection.Invoke(brain, "Update");
            object stun = new();
            object secondStun = new();
            brain.Lock(stun);
            brain.Lock(secondStun);
            brain.Unlock(stun);
            TestReflection.Invoke(brain, "Update");
            Assert.AreEqual(2, brain.Context.Get<int>("Count"));

            brain.Unlock(secondStun);
            TestReflection.Invoke(brain, "Update");
            Assert.AreEqual(1, brain.Context.Get<int>("Count"));
            Assert.AreSame(target, brain.Context.Get<Entity>("Target"));
        }

        [Test]
        public void DisableAndEnable_RestartsExecutionWithoutClearingVariables()
        {
            Brain brain = CreateBrain();
            TestReflection.Invoke(brain, "Update");
            TestReflection.Invoke(brain, "Update");
            TestReflection.Invoke(brain, "OnDisable");
            Assert.AreEqual(2, brain.Context.Get<int>("Count"));
            TestReflection.Invoke(brain, "Update");
            Assert.AreEqual(1, brain.Context.Get<int>("Count"));
        }

        [Test]
        public void FindTarget_SelectsNearestOtherEntityAndClearsItsOwnVariableWhenNoneRemain()
        {
            Entity owner = m_objects.CreateComponent<Entity>();
            Entity near = m_objects.CreateComponent<Entity>();
            near.transform.position = new Vector3(1, 0, 0);
            near.gameObject.AddComponent<BoxCollider>();
            Entity far = m_objects.CreateComponent<Entity>();
            far.transform.position = new Vector3(4, 0, 0);
            far.gameObject.AddComponent<BoxCollider>();
            owner.gameObject.AddComponent<BoxCollider>();
            owner.gameObject.layer = near.gameObject.layer = far.gameObject.layer = 30;
            Physics.SyncTransforms();
            BrainContext context = new(owner, new BrainVariable[] { new BrainEntity { Name = "Target" } });
            FindTarget find = new()
            {
                TargetVariable = "Target",
                Radius = new BrainValue<float> { Constant = 10 },
                Layers = 1 << 30
            };

            Assert.AreEqual(BrainStatus.Success, find.Tick(context));
            Assert.AreSame(near, context.Get<Entity>("Target"));
            near.gameObject.SetActive(false);
            far.gameObject.SetActive(false);
            Physics.SyncTransforms();
            find.Reset(context);
            Assert.AreEqual(BrainStatus.Failure, find.Tick(context));
            Assert.IsNull(context.Get<Entity>("Target"));
        }

        private Brain CreateBrain(params BrainVariable[] overrides)
        {
            Brain brain = m_objects.CreateComponent<Brain>();
            TestReflection.SetField(brain, "m_graph", m_graph);
            TestReflection.SetField(brain, "m_variableOverrides", overrides);
            TestReflection.Invoke(brain, "Start");
            return brain;
        }
    }
}
