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

        [Test]
        public void Invulnerable_IgnoresDamage()
        {
            Awake();
            m_health.Invulnerable = true;

            m_health.TakeDamage(new Damage(50));

            Assert.AreEqual(BaseMaxHealth, m_health.CurrentHealth);
        }

        [Test]
        public void LockedSystem_IgnoresDamageUntilEveryOwnerUnlocks()
        {
            Awake();
            object stun = new();
            object ability = new();
            m_health.Lock(stun);
            m_health.Lock(ability);

            m_health.Unlock(stun);
            m_health.TakeDamage(new Damage(10));
            Assert.IsFalse(m_health.IsSystemActive);
            Assert.AreEqual(BaseMaxHealth, m_health.CurrentHealth);

            m_health.Unlock(ability);
            m_health.TakeDamage(new Damage(10));
            Assert.IsTrue(m_health.IsSystemActive);
            Assert.AreEqual(90, m_health.CurrentHealth);
        }

        [Test]
        public void Shield_AbsorbsDamageFirst_AndOverflowReachesHealth()
        {
            TestReflection.SetField(m_health, "m_maxShield", 30f);
            Awake();
            int broken = 0;
            m_health.OnShieldBroken += () => broken++;

            m_health.TakeDamage(new Damage(20));
            Assert.AreEqual(10, m_health.CurrentShield);
            Assert.AreEqual(BaseMaxHealth, m_health.CurrentHealth);
            Assert.AreEqual(0, broken);

            m_health.TakeDamage(new Damage(25));
            Assert.AreEqual(0, m_health.CurrentShield);
            Assert.AreEqual(85, m_health.CurrentHealth);
            Assert.AreEqual(1, broken);

            m_health.TakeDamage(new Damage(5));
            Assert.AreEqual(80, m_health.CurrentHealth);
            Assert.AreEqual(1, broken);
        }

        [Test]
        public void RestoreShield_ClampsToMax_AndIsIgnoredWhenDead()
        {
            TestReflection.SetField(m_health, "m_maxShield", 30f);
            Awake();
            m_health.TakeDamage(new Damage(20));

            m_health.RestoreShield(15);
            Assert.AreEqual(25, m_health.CurrentShield);
            m_health.RestoreShield(15);
            Assert.AreEqual(30, m_health.CurrentShield);

            m_health.TakeDamage(new Damage(200));
            m_health.RestoreShield(15);
            Assert.AreEqual(0, m_health.CurrentShield);
        }

        [Test]
        public void NoShield_DamageGoesStraightToHealth()
        {
            Awake();
            m_health.TakeDamage(new Damage(10));
            Assert.AreEqual(0, m_health.MaxShield);
            Assert.AreEqual(90, m_health.CurrentHealth);
        }
    }
}
