using NUnit.Framework;
using RogueLikeEngine.Systems.Stats;

namespace RogueLikeEngine.Tests
{
    public class StatTests
    {
        private TestObjects m_objects;
        private StatsStore m_store;
        private StatDefinition m_damage;
        private StatDefinition m_strength;

        [SetUp]
        public void SetUp()
        {
            m_objects = new TestObjects();
            m_store = new StatsStore();
            m_damage = m_objects.CreateAsset<StatDefinition>();
            m_damage.statName = "Damage";
            m_strength = m_objects.CreateAsset<StatDefinition>();
            m_strength.statName = "Strength";
        }

        [TearDown]
        public void TearDown() => m_objects.Dispose();

        private Stat Damage => m_store.GetOrCreateStat(m_damage);
        private Stat Strength => m_store.GetOrCreateStat(m_strength);

        [Test]
        public void GetOrCreateStat_SameDefinition_ReturnsSameInstance()
        {
            Assert.AreSame(Damage, Damage);
            Assert.AreNotSame(Damage, Strength);
        }

        [Test]
        public void NewStat_IsZero()
        {
            Assert.AreEqual(0, Damage.FinalValue);
            Assert.AreEqual(0f, Damage.FinalFloatValue);
        }

        [Test]
        public void FlatModifiers_AreSummed()
        {
            m_store.AddModifier(new TestModifier(m_damage, 10));
            m_store.AddModifier(new TestModifier(m_damage, 5));

            Assert.AreEqual(15, Damage.FinalValue);
        }

        [Test]
        public void SameModifierInstance_IsOnlyAddedOnce()
        {
            TestModifier modifier = new(m_damage, 10);
            m_store.AddModifier(modifier);
            m_store.AddModifier(modifier);

            Assert.AreEqual(10, Damage.FinalValue);
        }

        [Test]
        public void Permanence_SplitsPermanentAndTemporaryValues()
        {
            m_store.AddModifier(new TestModifier(m_damage, 10, permanence: StatPermanence.Permanent));
            m_store.AddModifier(new TestModifier(m_damage, 3, permanence: StatPermanence.Temporary));
            m_store.AddModifier(new TestModifier(m_damage, 2, permanence: StatPermanence.FinalValue));

            Assert.AreEqual(12f, Damage.PermanentValue);
            Assert.AreEqual(3f, Damage.TemporaryValue);
            Assert.AreEqual(15, Damage.FinalValue);
        }

        [Test]
        public void RemoveModifier_RevertsValue()
        {
            TestModifier modifier = new(m_damage, 10);
            m_store.AddModifier(new TestModifier(m_damage, 5));
            m_store.AddModifier(modifier);

            m_store.RemoveModifier(modifier);

            Assert.AreEqual(5, Damage.FinalValue);
        }

        [Test]
        public void RemoveModifier_NotAdded_DoesNothing()
        {
            m_store.AddModifier(new TestModifier(m_damage, 5));

            Assert.DoesNotThrow(() => m_store.RemoveModifier(new TestModifier(m_damage, 10)));
            Assert.AreEqual(5, Damage.FinalValue);
        }

        [Test]
        public void InactiveModifier_IsIgnoredAfterRecalculate()
        {
            TestModifier modifier = new(m_damage, 10);
            m_store.AddModifier(modifier);

            modifier.IsActive = false;
            m_store.ReCalculate(modifier);
            Assert.AreEqual(0, Damage.FinalValue);

            modifier.IsActive = true;
            m_store.ReCalculate(modifier);
            Assert.AreEqual(10, Damage.FinalValue);
        }

        [Test]
        public void FinalValue_FloorsFractionalValue()
        {
            m_store.AddModifier(new TestModifier(m_damage, 2.7f));

            Assert.AreEqual(2, Damage.FinalValue);
            Assert.AreEqual(2.7f, Damage.FinalFloatValue, 1e-5f);
        }

        [Test]
        public void ForceCap_LimitsValueFromAbove()
        {
            m_store.AddModifier(new TestModifier(m_damage, 50));
            m_store.AddModifier(new TestModifier(m_damage, 30, StatModifierMode.ForceCap));

            Assert.AreEqual(30, Damage.FinalValue);
        }

        [Test]
        public void ForceCap_AboveValue_HasNoEffect()
        {
            m_store.AddModifier(new TestModifier(m_damage, 50));
            m_store.AddModifier(new TestModifier(m_damage, 100, StatModifierMode.ForceCap));

            Assert.AreEqual(50, Damage.FinalValue);
        }

        [Test]
        public void RateIncrease_AddedBeforeFlat_ScalesIt()
        {
            m_store.AddModifier(new TestModifier(m_damage, 0.5f, StatModifierMode.RateIncrease));
            m_store.AddModifier(new TestModifier(m_damage, 10));

            Assert.AreEqual(15, Damage.FinalValue);
        }

        [Test]
        public void RateIncrease_AddedAfterFlat_ScalesIt()
        {
            m_store.AddModifier(new TestModifier(m_damage, 10));
            m_store.AddModifier(new TestModifier(m_damage, 0.5f, StatModifierMode.RateIncrease));

            Assert.AreEqual(15, Damage.FinalValue);
        }

        [Test]
        public void RateIncrease_IgnoresPriority()
        {
            m_store.AddModifier(new TestModifier(m_damage, 10, priority: 10));
            m_store.AddModifier(new TestModifier(m_damage, 0.5f, StatModifierMode.RateIncrease, priority: -10));

            Assert.AreEqual(15, Damage.FinalValue);
        }

        [Test]
        public void RateIncreases_AreAddedTogether()
        {
            m_store.AddModifier(new TestModifier(m_damage, 10));
            m_store.AddModifier(new TestModifier(m_damage, 0.5f, StatModifierMode.RateIncrease));
            m_store.AddModifier(new TestModifier(m_damage, 0.5f, StatModifierMode.RateIncrease));

            Assert.AreEqual(2f, Damage.IncreasePercentage);
            Assert.AreEqual(20, Damage.FinalValue);
        }

        [Test]
        public void RateIncrease_ScalesPermanentAndTemporaryValues()
        {
            m_store.AddModifier(new TestModifier(m_damage, 10, permanence: StatPermanence.Permanent));
            m_store.AddModifier(new TestModifier(m_damage, 4, permanence: StatPermanence.Temporary));
            m_store.AddModifier(new TestModifier(m_damage, 0.5f, StatModifierMode.RateIncrease));

            Assert.AreEqual(15f, Damage.PermanentValue);
            Assert.AreEqual(6f, Damage.TemporaryValue);
        }

        [Test]
        public void RateIncrease_Removed_RevertsScaling()
        {
            TestModifier rate = new(m_damage, 0.5f, StatModifierMode.RateIncrease);
            m_store.AddModifier(new TestModifier(m_damage, 10));
            m_store.AddModifier(rate);

            m_store.RemoveModifier(rate);

            Assert.AreEqual(10, Damage.FinalValue);
        }

        [Test]
        public void ForceCap_Multiple_LowestWinsRegardlessOfOrder()
        {
            m_store.AddModifier(new TestModifier(m_damage, 50));
            m_store.AddModifier(new TestModifier(m_damage, 20, StatModifierMode.ForceCap));
            m_store.AddModifier(new TestModifier(m_damage, 30, StatModifierMode.ForceCap));

            Assert.AreEqual(20, Damage.FinalValue);
        }

        [Test]
        public void OnValueChanged_IsRaisedWhenModifierIsAdded()
        {
            int raised = 0;
            Damage.OnValueChanged += () => raised++;

            m_store.AddModifier(new TestModifier(m_damage, 10));

            Assert.AreEqual(1, raised);
        }

        [Test]
        public void StatLink_LinkedStatChange_UpdatesOwnerStat()
        {
            m_store.AddModifier(CreateLink(rate: 2));
            Assert.AreEqual(0, Damage.FinalValue);

            m_store.AddModifier(new TestModifier(m_strength, 5));

            Assert.AreEqual(10, Damage.FinalValue);
        }

        [Test]
        public void PreviewModifierValue_ReturnsContributionWithoutChangingState()
        {
            m_store.AddModifier(new TestModifier(m_strength, 5));
            m_store.AddModifier(CreateLink(rate: 2));
            int linksBefore = Strength.StatLinks.Count;
            int damageBefore = Damage.FinalValue;

            StatLinkModifier preview = CreateLink(rate: 3);
            float previewValue = Damage.PreviewModifierValue(preview);

            Assert.AreEqual(15f, previewValue);
            Assert.AreEqual(linksBefore, Strength.StatLinks.Count, "Preview must not register new stat links");
            Assert.AreEqual(damageBefore, Damage.FinalValue, "Preview must not change the stat");
            Assert.IsNull(preview.OwnerStat, "Preview must not configure the previewed modifier");
        }

        [Test]
        public void PreviewModifierValue_RegularModifier_ReturnsItsPreviewValue()
        {
            Assert.AreEqual(7f, Damage.PreviewModifierValue(new TestModifier(m_damage, 7)));
        }

        private StatLinkModifier CreateLink(float rate)
        {
            StatLinkModifier link = new() { linkedStatDefinition = m_strength };
            TestReflection.SetField(link, "targetStat", m_damage);
            TestReflection.SetField(link, "increaseRate", rate);
            return link;
        }

        private class TestModifier : AbstractStatModifier
        {
            private readonly float m_value;
            private readonly int m_priority;

            public TestModifier(StatDefinition stat, float value,
                StatModifierMode mode = StatModifierMode.FlatValue,
                StatPermanence permanence = StatPermanence.Permanent,
                int priority = 0)
            {
                targetStat = stat;
                m_value = value;
                this.mode = mode;
                statPermanence = permanence;
                m_priority = priority;
            }

            public override float Value => m_value;
            public override float ValuePreview => m_value;
            public override int Priority => m_priority;
        }
    }
}
