using System;
using NUnit.Framework;
using RogueLikeEngine.Systems.Brains;
using RogueLikeEngine.Systems.Brains.Nodes;

namespace RogueLikeEngine.Tests
{
    public class BrainExecutionTests
    {
        private BrainContext m_context;

        [SetUp]
        public void SetUp() => m_context = new BrainContext(null, new BrainVariable[] { new BrainInt { Name = "Count" } });

        [Test]
        public void Sequence_WaitsAndDoesNotRepeatFinishedChildren()
        {
            Probe first = new() { Result = BrainStatus.Success };
            Probe second = new();
            Probe third = new() { Result = BrainStatus.Success };
            BrainSequence sequence = new() { Children = new BrainNode[] { first, second, third } };

            Assert.AreEqual(BrainStatus.Running, sequence.Tick(m_context));
            Assert.AreEqual(BrainStatus.Running, sequence.Tick(m_context));
            Assert.AreEqual(1, first.Ticks);
            Assert.AreEqual(2, second.Ticks);
            Assert.AreEqual(0, third.Ticks);

            second.Result = BrainStatus.Success;
            Assert.AreEqual(BrainStatus.Success, sequence.Tick(m_context));
            Assert.AreEqual(1, third.Ticks);
        }

        [Test]
        public void Selector_TriesFallbackAndKeepsRunningChild()
        {
            Probe first = new() { Result = BrainStatus.Failure };
            Probe second = new();
            Probe third = new();
            BrainSelector selector = new() { Children = new BrainNode[] { first, second, third } };

            selector.Tick(m_context);
            selector.Tick(m_context);
            Assert.AreEqual(1, first.Ticks);
            Assert.AreEqual(2, second.Ticks);
            Assert.AreEqual(0, third.Ticks);
        }

        [Test]
        public void Parallel_TicksRunningChildrenButDoesNotRepeatCompletedActions()
        {
            Probe first = new() { Result = BrainStatus.Success };
            Probe second = new();
            BrainParallel parallel = new() { Children = new BrainNode[] { first, second } };

            parallel.Tick(m_context);
            parallel.Tick(m_context);
            Assert.AreEqual(1, first.Ticks);
            Assert.AreEqual(2, second.Ticks);
            second.Result = BrainStatus.Success;
            Assert.AreEqual(BrainStatus.Success, parallel.Tick(m_context));
        }

        [Test]
        public void Parallel_FailureStopsOtherActiveActionsExactlyOnce()
        {
            Probe first = new();
            Probe second = new();
            BrainParallel parallel = new() { Children = new BrainNode[] { first, second } };
            parallel.Tick(m_context);
            second.Result = BrainStatus.Failure;

            Assert.AreEqual(BrainStatus.Failure, parallel.Tick(m_context));
            parallel.Reset(m_context);
            Assert.AreEqual(1, first.Stops);
            Assert.AreEqual(1, second.Stops);
        }

        [Test]
        public void Reset_RestartsFromBeginningAndPreservesVariables()
        {
            Probe first = new() { Result = BrainStatus.Success };
            Probe second = new();
            BrainSequence sequence = new() { Children = new BrainNode[] { first, second } };
            sequence.Tick(m_context);
            m_context.Set("Count", 42);

            sequence.Reset(m_context);
            sequence.Tick(m_context);
            Assert.AreEqual(2, first.Starts);
            Assert.AreEqual(2, second.Starts);
            Assert.AreEqual(42, m_context.Get<int>("Count"));
        }

        [Test]
        public void ResetDuringAction_DoesNotRunTheNextAction()
        {
            Probe first = new() { Result = BrainStatus.Success };
            Probe second = new();
            BrainSequence sequence = new() { Children = new BrainNode[] { first, second } };
            first.OnTickCallback = () => sequence.Reset(m_context);

            Assert.AreEqual(BrainStatus.Failure, sequence.Tick(m_context));
            Assert.AreEqual(0, second.Ticks);
            Assert.AreEqual(1, first.Stops);
            first.OnTickCallback = null;
            Assert.AreEqual(BrainStatus.Running, sequence.Tick(m_context));
            Assert.AreEqual(2, first.Starts);
        }

        [Test]
        public void VariableBinding_ReadsLatestValueAndRejectsWrongTypes()
        {
            BrainValue<int> value = new() { Variable = "Count" };
            m_context.Set("Count", 7);
            Assert.AreEqual(7, value.Get(m_context));
            m_context.Set("Count", 9);
            Assert.AreEqual(9, value.Get(m_context));
            Assert.Throws<InvalidCastException>(() => m_context.Set("Count", true));
        }
    }
}
