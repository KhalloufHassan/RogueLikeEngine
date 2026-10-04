using NUnit.Framework;
using RogueLikeEngine.Systems.Entities;
using RogueLikeEngine.Systems.Healths;
using RogueLikeEngine.Systems.Stats;
using RogueLikeEngine.Systems.Weapons;

namespace RogueLikeEngine.Tests
{
    public class HealthTests
    {
        private const int BaseMaxHealth = 100;

        private TestObjects m_objects;
        private Entity m_entity;
        private Health m_health;
        private StatDefinition m_maxHealthStat;
        private int m_deaths;

        [SetUp]
        public void SetUp()
        {
            m_objects = new TestObjects();
            m_deaths = 0;
            m_maxHealthStat = m_objects.CreateAsset<StatDefinition>();
            m_entity = m_objects.CreateComponent<Entity>();
            m_health = m_entity.gameObject.AddComponent<Health>();
            TestReflection.SetField(m_health, "m_entity", m_entity);
            TestReflection.SetField(m_health, "m_maxHealth", BaseMaxHealth);
            TestReflection.SetField(m_health, "m_maxHealthStatDefinition", m_maxHealthStat);
            // Destroy can't be called in edit mode, death is observed through OnDeath instead
            TestReflection.SetField(m_health, "m_destroyEntityOnDeath", false);
            m_health.OnDeath += () => m_deaths++;
        }

        [TearDown]
        public void TearDown() => m_objects.Dispose();

        private void Awake() => TestReflection.Invoke(m_health, "Awake");

        private BasicStatModifier AddMaxHealth(float value)
        {
            BasicStatModifier modifier = TestModifiers.Flat(m_maxHealthStat, value);
            m_entity.StatsStore.AddModifier(modifier);
            return modifier;
        }

        [Test]
        public void Awake_StartsAtMaxHealthIncludingStat()
        {
            AddMaxHealth(20);
            Awake();

            Assert.AreEqual(120, m_health.MaxHealth);
            Assert.AreEqual(120, m_health.CurrentHealth);
        }

        [Test]
        public void NoStatDefinition_UsesBaseMaxHealth()
        {
            TestReflection.SetField(m_health, "m_maxHealthStatDefinition", null);
            Awake();

            Assert.AreEqual(BaseMaxHealth, m_health.MaxHealth);
            Assert.AreEqual(BaseMaxHealth, m_health.CurrentHealth);
        }

        [Test]
        public void MaxHealthGainedAfterAwake_RaisesMaxAndCurrent()
        {
            Awake();
            m_health.TakeDamage(new Damage(50));

            AddMaxHealth(20);

            Assert.AreEqual(120, m_health.MaxHealth);
            Assert.AreEqual(70, m_health.CurrentHealth);
        }

        [Test]
        public void MaxHealthLost_ClampsCurrentToNewMax()
        {
            Awake();

            AddMaxHealth(-30);

            Assert.AreEqual(70, m_health.MaxHealth);
            Assert.AreEqual(70, m_health.CurrentHealth);
        }

        [Test]
        public void MaxHealthLost_BelowCurrent_KeepsCurrent()
        {
            Awake();
            m_health.TakeDamage(new Damage(50));

            AddMaxHealth(-30);

            Assert.AreEqual(50, m_health.CurrentHealth);
        }

        [Test]
        public void MaxHealthRemovedAndRegained_GrantsTheRegainedAmount()
        {
            BasicStatModifier bonus = AddMaxHealth(20);
            Awake();
            m_health.TakeDamage(new Damage(50));

            m_entity.StatsStore.RemoveModifier(bonus); // 100 max, current stays 70
            m_entity.StatsStore.AddModifier(bonus);    // 120 max, +20 granted

            Assert.AreEqual(120, m_health.MaxHealth);
            Assert.AreEqual(90, m_health.CurrentHealth);
        }

        [Test]
        public void TakeDamage_ReducesHealthAndRaisesEvents()
        {
            Awake();
            int changed = 0;
            Damage? received = null;
            m_health.OnHealthChanged += () => changed++;
            m_health.OnDamaged += d => received = d;

            m_health.TakeDamage(new Damage(30));

            Assert.AreEqual(70, m_health.CurrentHealth);
            Assert.AreEqual(1, changed);
            Assert.AreEqual(30, received?.Value);
        }

        [Test]
        public void TakeDamage_NegativeValue_DoesNotHeal()
        {
            Awake();
            m_health.TakeDamage(new Damage(30));

            m_health.TakeDamage(new Damage(-50));

            Assert.AreEqual(70, m_health.CurrentHealth);
        }

        [Test]
        public void LethalDamage_ClampsToZeroAndDiesOnce()
        {
            Awake();

            m_health.TakeDamage(new Damage(150));
            m_health.TakeDamage(new Damage(10));

            Assert.AreEqual(0, m_health.CurrentHealth);
            Assert.IsTrue(m_health.IsDead);
            Assert.AreEqual(1, m_deaths);
        }

        [Test]
        public void MaxHealthDroppingToZero_Kills()
        {
            Awake();

            AddMaxHealth(-BaseMaxHealth);

            Assert.IsTrue(m_health.IsDead);
            Assert.AreEqual(1, m_deaths);
        }

        [Test]
        public void TakeDamage_SystemInactive_IsIgnored()
        {
            Awake();
            m_health.IsSystemActive = false;

            m_health.TakeDamage(new Damage(30));

            Assert.AreEqual(BaseMaxHealth, m_health.CurrentHealth);
        }

        [Test]
        public void Heal_ClampsToMaxAndReportsHealedAmount()
        {
            Awake();
            m_health.TakeDamage(new Damage(30));
            int healed = 0;
            m_health.OnHealed += amount => healed = amount;

            m_health.Heal(50);

            Assert.AreEqual(BaseMaxHealth, m_health.CurrentHealth);
            Assert.AreEqual(30, healed);
        }

        [Test]
        public void Heal_AtFullHealth_RaisesNoEvent()
        {
            Awake();
            int events = 0;
            m_health.OnHealed += _ => events++;
            m_health.OnHealthChanged += () => events++;

            m_health.Heal(10);

            Assert.AreEqual(0, events);
        }

        [Test]
        public void Heal_WhenDead_IsIgnored()
        {
            Awake();
            m_health.TakeDamage(new Damage(BaseMaxHealth));

            m_health.Heal(50);

            Assert.AreEqual(0, m_health.CurrentHealth);
        }
    }
}
