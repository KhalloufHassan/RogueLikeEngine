using NUnit.Framework;
using RogueLikeEngine.Systems.Brains;
using RogueLikeEngine.Systems.Brains.Nodes;
using RogueLikeEngine.Systems.Entities;
using RogueLikeEngine.Systems.Movements;
using RogueLikeEngine.Systems.Stats;
using RogueLikeEngine.Systems.Weapons;
using UnityEngine;

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

        [Test]
        public void Equip_ReplacesTheCurrentWeaponAndRaisesTheEvent()
        {
            int changes = 0;
            m_weaponsSystem.OnWeaponChanged += () => changes++;
            m_weaponsSystem.Equip(m_weapon);
            Assert.AreSame(m_weapon, m_weaponsSystem.CurrentWeapon.WeaponData);
            m_weaponsSystem.Equip(null);
            Assert.IsNull(m_weaponsSystem.CurrentWeapon);
            Assert.AreEqual(2, changes);
        }

        [Test]
        public void Inaccuracy_IsReducedByTheAccuracyStatAsAPercentage_DownToZero()
        {
            m_weapon.baseInaccuracy = 20;
            StatDefinition accuracyStat = m_objects.CreateAsset<StatDefinition>();
            TestReflection.SetField(m_weaponsSystem, "m_accuracyStatDefinition", accuracyStat);
            Assert.AreEqual(20f, m_weaponsSystem.CalculatedInaccuracy(m_weapon));
            AddFlat(accuracyStat, 25);
            Assert.AreEqual(15f, m_weaponsSystem.CalculatedInaccuracy(m_weapon));
            AddFlat(accuracyStat, 100);
            Assert.AreEqual(0f, m_weaponsSystem.CalculatedInaccuracy(m_weapon));
        }

        [Test]
        public void GunPoints_CycleThroughTheirPoints_OrUseTheirOwnTransform()
        {
            GunPoints gunPoints = m_objects.CreateComponent<GunPoints>();
            gunPoints.transform.position = Vector3.down;
            Assert.AreEqual(Vector3.down, gunPoints.Position);
            gunPoints.Advance();
            Assert.AreEqual(Vector3.down, gunPoints.Position);

            Transform left = m_objects.Track(new GameObject("Left")).transform;
            Transform right = m_objects.Track(new GameObject("Right")).transform;
            left.position = Vector3.left;
            right.position = Vector3.right;
            TestReflection.SetField(gunPoints, "m_points", new[] { left, right });
            Assert.AreEqual(Vector3.left, gunPoints.Position);
            gunPoints.Advance();
            Assert.AreEqual(Vector3.right, gunPoints.Position);
            gunPoints.Advance();
            Assert.AreEqual(Vector3.left, gunPoints.Position);
        }

        [Test]
        public void GunPoints_RotateTheirDirectionOnThePlane()
        {
            GunPoints gunPoints = m_objects.CreateComponent<GunPoints>();
            Assert.That(Vector3.Angle(Vector3.right, gunPoints.GetDirection(0)), Is.LessThan(1e-3f));
            Assert.That(Vector3.Angle(Vector3.up, gunPoints.GetDirection(90)), Is.LessThan(1e-3f));
        }

        [Test]
        public void ProjectileCountMagazineAndReload_AreScaledByTheirStats()
        {
            m_weapon.baseProjectileCount = 2;
            m_weapon.baseMagazineSize = 10;
            m_weapon.baseReloadDuration = 2;
            StatDefinition countStat = m_objects.CreateAsset<StatDefinition>();
            StatDefinition magazineStat = m_objects.CreateAsset<StatDefinition>();
            StatDefinition reloadStat = m_objects.CreateAsset<StatDefinition>();
            TestReflection.SetField(m_weaponsSystem, "m_projectileCountStatDefinition", countStat);
            TestReflection.SetField(m_weaponsSystem, "m_magazineSizeStatDefinition", magazineStat);
            TestReflection.SetField(m_weaponsSystem, "m_reloadSpeedStatDefinition", reloadStat);
            AddFlat(countStat, 1);
            AddFlat(magazineStat, 5);
            AddFlat(reloadStat, 25);

            Assert.AreEqual(3, m_weaponsSystem.CalculatedProjectileCount(m_weapon));
            Assert.AreEqual(15, m_weaponsSystem.CalculatedMagazineSize(m_weapon));
            Assert.AreEqual(1.5f, m_weaponsSystem.CalculatedReloadDuration(m_weapon));
            m_weapon.baseMagazineSize = 0;
            Assert.AreEqual(0, m_weaponsSystem.CalculatedMagazineSize(m_weapon), "Unlimited ammo stays unlimited");
        }

        [Test]
        public void Weight_SlowsMovementOnlyWhileAiming()
        {
            Movement movement = m_entity.gameObject.AddComponent<Movement>();
            TestReflection.SetField(movement, "m_entity", m_entity);
            TestReflection.SetField(m_entity, "m_weaponsSystem", m_weaponsSystem);
            m_weapon.weight = .4f;
            m_weaponsSystem.Equip(m_weapon);
            Assert.AreEqual(5f, movement.Speed);
            m_weaponsSystem.Aim(Vector3.forward);
            Assert.AreEqual(3f, movement.Speed, 1e-4f);
        }

        [Test]
        public void ReloadWeaponNode_FailsWithoutAWeapon_RunsWhileReloading_AndSucceedsWhenFull()
        {
            TestReflection.SetField(m_entity, "m_weaponsSystem", m_weaponsSystem);
            BrainContext context = new(m_entity, System.Array.Empty<BrainVariable>());
            Assert.AreEqual(BrainStatus.Failure, new ReloadWeapon().Tick(context));

            m_weaponsSystem.Equip(m_weapon);
            Assert.AreEqual(BrainStatus.Success, new ReloadWeapon().Tick(context), "Unlimited ammo needs no reload");

            m_weapon.baseMagazineSize = 3;
            m_weaponsSystem.Equip(m_weapon);
            Assert.AreEqual(BrainStatus.Success, new ReloadWeapon().Tick(context), "Full magazine");
            m_weaponsSystem.CurrentWeapon.Ammo = 0;
            Assert.AreEqual(BrainStatus.Running, new ReloadWeapon().Tick(context));
            Assert.IsTrue(m_weaponsSystem.IsReloading);
        }

        [Test]
        public void FireAtTarget_EndsTheBurstOnceTheWeaponReloads()
        {
            TestReflection.SetField(m_entity, "m_weaponsSystem", m_weaponsSystem);
            m_weapon.baseMagazineSize = 3;
            m_weaponsSystem.Equip(m_weapon);
            Entity target = m_objects.CreateComponent<Entity>();
            BrainContext context = new(m_entity, System.Array.Empty<BrainVariable>());
            FireAtTarget fire = new() { Target = new BrainValue<Entity> { Constant = target } };
            Assert.AreEqual(BrainStatus.Running, fire.Tick(context));
            m_weaponsSystem.CurrentWeapon.Ammo = 0;
            m_weaponsSystem.Reload();
            Assert.AreEqual(BrainStatus.Success, fire.Tick(context));
            Assert.IsFalse(m_weaponsSystem.IsFireHeld);
        }

        private void AddFlat(StatDefinition stat, float value) => m_entity.StatsStore.AddModifier(TestModifiers.Flat(stat, value));
    }
}
