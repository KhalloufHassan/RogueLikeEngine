using NUnit.Framework;
using RogueLikeEngine.Systems.Stats;

namespace RogueLikeEngine.Tests
{
    public class IntTrackerBasedStatModifierTests
    {
        private TestObjects m_objects;
        private StatsStore m_store;
        private StatDefinition m_damage;
        private IntTracker m_kills;

        [SetUp]
        public void SetUp()
        {
            m_objects = new TestObjects();
            m_store = new StatsStore();
            m_damage = m_objects.CreateAsset<StatDefinition>();
            m_kills = m_objects.CreateAsset<IntTracker>();
        }

        [TearDown]
        public void TearDown() => m_objects.Dispose();

        private Stat Damage => m_store.GetOrCreateStat(m_damage);

        private IntTrackerBasedStatModifier Create(float rate, IntTracker tracker)
        {
            IntTrackerBasedStatModifier modifier = new();
            TestReflection.SetField(modifier, "targetStat", m_damage);
            TestReflection.SetField(modifier, "m_tracker", tracker);
            TestReflection.SetField(modifier, "m_increaseRate", rate);
            return modifier;
        }

        [Test]
        public void IsSerializable_SoSerializeReferenceCanStoreIt()
        {
            Assert.IsTrue(typeof(IntTrackerBasedStatModifier).IsSerializable);
        }

        [Test]
        public void Value_IsTrackerValueTimesRate()
        {
            m_kills.Value = 4;

            m_store.AddModifier(Create(rate: 2, m_kills));

            Assert.AreEqual(8, Damage.FinalValue);
        }

        [Test]
        public void TrackerChange_RecalculatesTheStat()
        {
            m_store.AddModifier(Create(rate: 2, m_kills));

            m_kills.Value = 3;

            Assert.AreEqual(6, Damage.FinalValue);
        }

        [Test]
        public void RemovedModifier_StopsListeningToTheTracker()
        {
            IntTrackerBasedStatModifier modifier = Create(rate: 2, m_kills);
            m_store.AddModifier(modifier);
            m_store.RemoveModifier(modifier);
            int recalculations = 0;
            Damage.OnValueChanged += () => recalculations++;

            m_kills.Value = 10;

            Assert.AreEqual(0, recalculations);
            Assert.AreEqual(0, Damage.FinalValue);
        }

        [Test]
        public void NoTracker_ContributesNothing()
        {
            IntTrackerBasedStatModifier modifier = Create(rate: 2, null);

            Assert.DoesNotThrow(() => m_store.AddModifier(modifier));
            Assert.AreEqual(0, Damage.FinalValue);
            Assert.AreEqual(0f, modifier.ValuePreview);
        }
    }
}
