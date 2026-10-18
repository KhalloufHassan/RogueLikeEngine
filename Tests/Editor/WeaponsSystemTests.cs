using NUnit.Framework;
using RogueLikeEngine.Systems.Brains;
using RogueLikeEngine.Systems.Brains.Nodes;
using RogueLikeEngine.Systems.Entities;
using RogueLikeEngine.Systems.Stats;
using RogueLikeEngine.Systems.Weapons;

namespace RogueLikeEngine.Tests
{
    public class WeaponsSystemTests
    {
        private TestObjects m_objects;
        private Entity m_entity;
        private WeaponsSystem m_weaponsSystem;
        private WeaponData m_weapon;
        private StatDefinition m_damageStat;
        private StatDefinition m_rangeStat;

        [SetUp]
        public void SetUp()
        {
            m_objects = new TestObjects();
            m_entity = m_objects.CreateComponent<Entity>();
            m_weaponsSystem = m_entity.gameObject.AddComponent<WeaponsSystem>();
            TestReflection.SetField(m_weaponsSystem, "m_entity", m_entity);

            m_damageStat = m_objects.CreateAsset<StatDefinition>();
            m_rangeStat = m_objects.CreateAsset<StatDefinition>();

            m_weapon = m_objects.CreateAsset<WeaponData>();
            m_weapon.baseDamage = 3;
            m_weapon.baseFireRate = 2;
            m_weapon.baseRange = 10;
            m_weapon.damageStat = m_damageStat;
        }

        [TearDown]
        public void TearDown() => m_objects.Dispose();

        [TestCase(false)]
        [TestCase(true)]
        public void HeldFireWorksWithoutAReadyWeaponAndClearsOnReleaseOrCancel(bool cancel)
        {
            IWeaponsSystem weapons = m_weaponsSystem;
            int shots = 0;
            weapons.OnFire += () => shots++;
            Assert.IsFalse(weapons.CanFire);
            weapons.BeginFire();
            weapons.Fire();
            Assert.IsTrue(weapons.IsFireHeld);
            Assert.AreEqual(0, shots);
            if (cancel) weapons.CancelInput();
            else weapons.EndFire();
            Assert.IsFalse(weapons.IsFireHeld);
        }

        [Test]
        public void LockCancelsHeldFireAndUnlockDoesNotRestartIt()
        {
            m_weaponsSystem.BeginFire();
            m_weaponsSystem.Lock(this);
            Assert.IsFalse(m_weaponsSystem.IsFireHeld);
            m_weaponsSystem.BeginFire();
            Assert.IsFalse(m_weaponsSystem.IsFireHeld);
            m_weaponsSystem.Unlock(this);
            Assert.IsFalse(m_weaponsSystem.IsFireHeld);
            m_weaponsSystem.BeginFire();
            Assert.IsTrue(m_weaponsSystem.IsFireHeld);
            m_weaponsSystem.IsSystemActive = false;
            Assert.IsFalse(m_weaponsSystem.IsFireHeld);
        }

        [Test]
        public void FireAtTargetStartsHeldFireAndCancelsOnInterruption()
        {
            TestReflection.SetField(m_entity, "m_weaponsSystem", m_weaponsSystem);
            Entity target = m_objects.CreateComponent<Entity>();
            BrainContext context = new(m_entity, System.Array.Empty<BrainVariable>());
            FireAtTarget fire = new() { Target = new BrainValue<Entity> { Constant = target } };
            Assert.AreEqual(BrainStatus.Running, fire.Tick(context));
            Assert.IsTrue(m_weaponsSystem.IsFireHeld);
            fire.Reset(context);
            Assert.IsFalse(m_weaponsSystem.IsFireHeld);
            fire.Target.Constant = null;
            Assert.AreEqual(BrainStatus.Failure, fire.Tick(context));
            Assert.IsFalse(m_weaponsSystem.IsFireHeld);
        }

        [Test]
        public void Damage_AddsDamageStat()
        {
            AddFlat(m_damageStat, 100);

            Assert.AreEqual(103, m_weaponsSystem.CalculatedProjectileDamage(m_weapon));
        }

        [Test]
        public void Range_IsNotAffectedByDamageStat()
        {
            AddFlat(m_damageStat, 100);

            Assert.AreEqual(10f, m_weaponsSystem.CalculatedProjectileRange(m_weapon));
        }

        [Test]
        public void Range_AddsRangeStat()
        {
            TestReflection.SetField(m_weaponsSystem, "m_rangeStatDefinition", m_rangeStat);
            AddFlat(m_rangeStat, 5);

            Assert.AreEqual(15f, m_weaponsSystem.CalculatedProjectileRange(m_weapon));
        }

        [Test]
        public void ProjectileSpeed_IsScaledByTheSpeedStatAsAPercentage()
        {
            m_weapon.baseProjectileSpeed = 10;
            StatDefinition speedStat = m_objects.CreateAsset<StatDefinition>();
            TestReflection.SetField(m_weaponsSystem, "m_projectileSpeedStatDefinition", speedStat);
            AddFlat(speedStat, 50);

            Assert.AreEqual(15f, m_weaponsSystem.CalculatedProjectileSpeed(m_weapon));
        }

        [Test]
        public void MissingStatDefinitions_FallBackToBaseValues()
        {
            m_weapon.damageStat = null;

            Assert.AreEqual(3, m_weaponsSystem.CalculatedProjectileDamage(m_weapon));
            Assert.AreEqual(2f, m_weaponsSystem.CalculatedFireRate(m_weapon));
            Assert.AreEqual(10f, m_weaponsSystem.CalculatedProjectileRange(m_weapon));
            Assert.AreEqual(m_weapon.baseProjectileSpeed, m_weaponsSystem.CalculatedProjectileSpeed(m_weapon));
        }

        private void AddFlat(StatDefinition stat, float value) => m_entity.StatsStore.AddModifier(TestModifiers.Flat(stat, value));
    }
}
