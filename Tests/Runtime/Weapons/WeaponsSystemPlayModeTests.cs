using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using RogueLikeEngine.Systems.Entities;
using RogueLikeEngine.Systems.Weapons;
using RogueLikeEngine.Vfx;
using UnityEngine;
using UnityEngine.TestTools;

namespace RogueLikeEngine.Tests.PlayMode
{
    public class WeaponsSystemPlayModeTests
    {
        private readonly List<Object> m_objects = new();
        private WeaponsSystem m_weapons;
        private RecordingWeaponBehavior m_behavior;
        private int m_shots;

        [SetUp]
        public void SetUp()
        {
            GameObject owner = Track(new GameObject("Owner"));
            owner.SetActive(false);
            owner.AddComponent<Rigidbody>().isKinematic = true;
            Entity entity = owner.AddComponent<Entity>();
            m_weapons = owner.AddComponent<WeaponsSystem>();
            SetField(m_weapons, "m_entity", entity);
            SetField(entity, "m_weaponsSystem", m_weapons);

            GameObject muzzle = new("Muzzle");
            muzzle.transform.SetParent(owner.transform);
            muzzle.transform.localPosition = Vector3.forward;
            SetField(m_weapons, "m_gunPoints", muzzle.AddComponent<GunPoints>());
            owner.SetActive(true);

            m_behavior = new RecordingWeaponBehavior();
            m_shots = 0;
            m_weapons.OnFire += () => m_shots++;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Projectile projectile in m_behavior.Projectiles)
                if (projectile) Object.Destroy(projectile.transform.parent ? projectile.transform.parent.gameObject : projectile.gameObject);
            foreach (Object obj in m_objects)
                if (obj) Object.Destroy(obj);
            m_objects.Clear();
        }

        [Test]
        public void Shot_SpreadsItsProjectilesEvenly_AndCallsTheBehavior()
        {
            WeaponData weapon = Equip(w =>
            {
                w.baseProjectileCount = 3;
                w.spreadAngle = 20;
            });

            m_weapons.BeginFire();

            Assert.AreEqual(1, m_shots);
            Assert.AreEqual(1, m_behavior.Equipped);
            Assert.AreEqual(1, m_behavior.PreFires);
            Assert.AreEqual(1, m_behavior.PostFires);
            Assert.AreEqual(3, m_behavior.Projectiles.Count);
            float[] expected = { -10, 0, 10 };
            for (int i = 0; i < expected.Length; i++)
                Assert.AreEqual(expected[i], Vector3.SignedAngle(Vector3.forward, m_behavior.Projectiles[i].Direction, Vector3.up), 1e-3f);
            Assert.AreEqual(weapon.baseDamage, m_behavior.Projectiles[0].Damage.Value);
        }

        [Test]
        public void Shots_FlyAlongTheAim_NotTheGunPointsForward()
        {
            Equip(_ => { });
            m_weapons.Aim(Vector3.right);

            m_weapons.BeginFire();

            Assert.AreEqual(0, Vector3.Angle(Vector3.right, m_behavior.Projectiles[0].Direction), 1e-3f);
        }

        [UnityTest]
        public IEnumerator SemiAutomatic_FiresOncePerPress_AutomaticKeepsFiring()
        {
            Equip(w => w.automatic = false);
            m_weapons.BeginFire();
            yield return new WaitForSeconds(.2f);
            Assert.AreEqual(1, m_shots);
            m_weapons.EndFire();
            m_weapons.BeginFire();
            Assert.AreEqual(2, m_shots);
            m_weapons.EndFire();

            Equip(w => w.automatic = true);
            m_weapons.BeginFire();
            yield return new WaitForSeconds(.2f);
            Assert.That(m_shots, Is.GreaterThan(3));
        }

        [UnityTest]
        public IEnumerator Chargeable_FiresOnRelease_WithDamageScaledByTheCharge()
        {
            Equip(w =>
            {
                w.chargeable = true;
                w.chargeDuration = .2f;
                w.fullChargeDamageMultiplier = 3;
            });

            m_weapons.BeginFire();
            yield return new WaitForSeconds(.3f);
            Assert.AreEqual(0, m_shots, "Charging never fires while held");
            Assert.AreEqual(1, m_weapons.ChargeProgress);
            m_weapons.EndFire();
            Assert.AreEqual(1, m_shots);
            Assert.AreEqual(30, m_behavior.Projectiles[0].Damage.Value);
            Assert.AreEqual(0, m_weapons.ChargeProgress);

            yield return new WaitForSeconds(.05f);
            m_weapons.BeginFire();
            m_weapons.EndFire();
            Assert.AreEqual(2, m_shots);
            Assert.AreEqual(10, m_behavior.Projectiles[1].Damage.Value, "A tap fires uncharged");
        }

        [UnityTest]
        public IEnumerator EmptyMagazine_ReloadsAutomatically_AndBlocksFiringUntilRefilled()
        {
            Equip(w =>
            {
                w.baseMagazineSize = 2;
                w.baseReloadDuration = .2f;
            });
            int reloadsFinished = 0;
            m_weapons.OnReloadFinished += () => reloadsFinished++;

            m_weapons.BeginFire();
            yield return new WaitForSeconds(.1f);
            Assert.AreEqual(2, m_shots);
            Assert.IsTrue(m_weapons.IsReloading);
            Assert.IsFalse(m_weapons.CanFire);
            Assert.AreEqual(1, m_behavior.ReloadsStarted);
            m_weapons.EndFire();

            yield return new WaitForSeconds(.25f);
            Assert.IsFalse(m_weapons.IsReloading);
            Assert.AreEqual(2, m_weapons.CurrentWeapon.Ammo);
            Assert.AreEqual(1, reloadsFinished);
            Assert.AreEqual(1, m_behavior.ReloadsFinished);
            m_weapons.Reload();
            Assert.IsFalse(m_weapons.IsReloading, "A full magazine doesn't reload");
        }

        [UnityTest]
        public IEnumerator LockingCancelsTheReload_AndUnlockingRestartsIt()
        {
            Equip(w =>
            {
                w.baseMagazineSize = 1;
                w.baseReloadDuration = .2f;
            });
            m_weapons.Fire();
            yield return null;
            Assert.IsTrue(m_weapons.IsReloading);
            m_weapons.Lock(this);
            Assert.IsFalse(m_weapons.IsReloading);
            yield return new WaitForSeconds(.25f);
            Assert.AreEqual(0, m_weapons.CurrentWeapon.Ammo);
            m_weapons.Unlock(this);
            yield return null;
            Assert.IsTrue(m_weapons.IsReloading);
        }

        [UnityTest]
        public IEnumerator Projectiles_AreReusedFromTheWeaponsOwnPool()
        {
            Equip(w => w.baseRange = .5f);
            m_weapons.Fire();
            Projectile first = m_behavior.Projectiles[0];
            for (float time = 0; time < 1 && first.gameObject.activeSelf; time += Time.deltaTime)
                yield return null;
            Assert.IsFalse(first.gameObject.activeSelf, "the projectile should be back in its pool");

            m_weapons.Fire();
            Assert.AreSame(first, m_behavior.Projectiles[1]);
        }

        [UnityTest]
        public IEnumerator EveryShot_PlaysTheMuzzleFlashFollowingTheGunPoint()
        {
            PooledVfx flash = Track(VfxManagerPlayModeTests.CreateEffect("Muzzle flash", 1));
            SetField(m_weapons, "m_vfxManager", Track(ScriptableObject.CreateInstance<VfxManager>()));
            Equip(w =>
            {
                w.automatic = false;
                w.muzzleFlash = flash;
            });
            try
            {
                m_weapons.BeginFire();
                m_weapons.EndFire();
                m_weapons.transform.position += Vector3.right;
                yield return null;

                PooledVfx[] active = VfxManagerPlayModeTests.ActiveEffects;
                Assert.AreEqual(1, active.Length);
                Assert.That(Vector3.Distance(m_weapons.transform.position + Vector3.forward, active[0].transform.position), Is.LessThan(1e-4f));
            }
            finally
            {
                VfxManagerPlayModeTests.DestroyPools();
            }
        }

        private WeaponData Equip(System.Action<WeaponData> configure)
        {
            GameObject template = Track(new GameObject("ProjectileTemplate"));
            template.SetActive(false);
            template.AddComponent<Rigidbody>().useGravity = false;
            template.AddComponent<SphereCollider>().isTrigger = true;

            WeaponData weapon = Track(ScriptableObject.CreateInstance<WeaponData>());
            weapon.baseDamage = 10;
            weapon.baseFireRate = 100;
            weapon.baseRange = 100;
            weapon.projectilePrefab = template.AddComponent<Projectile>();
            weapon.behavior = m_behavior;
            configure(weapon);
            m_weapons.Equip(weapon);
            return weapon;
        }

        private T Track<T>(T obj) where T : Object
        {
            m_objects.Add(obj);
            return obj;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            for (System.Type type = target.GetType(); type != null; type = type.BaseType)
            {
                FieldInfo field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field == null) continue;
                field.SetValue(target, value);
                return;
            }
            throw new System.MissingFieldException(target.GetType().Name, fieldName);
        }
    }
}
