using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using RogueLikeEngine.Systems.Entities;
using RogueLikeEngine.Systems.Healths;
using RogueLikeEngine.Systems.Movements;
using RogueLikeEngine.Systems.Weapons;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace RogueLikeEngine.Tests.PlayMode
{
    public class CoverProjectilePlayModeTests
    {
        private static readonly Vector3 Origin = new(700, 0, 700);
        private readonly List<GameObject> m_objects = new();
        private Quaternion m_rotation;
        private Entity m_entity;
        private Health m_health;
        private CoverMovement m_movement;
        private CapsuleCollider m_body;
        private Cover m_cover;
        private BoxCollider m_wall;
        private HitInfo m_lastHit;
        private int m_hits;

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject obj in m_objects)
                if (obj) Object.Destroy(obj);
            m_objects.Clear();
        }

        [UnityTest]
        public IEnumerator FrontShotHitsCoverBeforeReachingTheEnemy() => CheckShot(new Vector3(0, 1.35f, -4), true);

        [UnityTest]
        public IEnumerator RearShotHitsCrouchedEnemy() => CheckShot(new Vector3(0, 1.35f, 4), false);

        [UnityTest]
        public IEnumerator SideShotHitsCrouchedEnemy() => CheckShot(new Vector3(4, 1.35f, .64f), false);

        [UnityTest]
        public IEnumerator RotatedCoverStillBlocksTheProtectedSide() => CheckShot(new Vector3(0, 1.35f, -4), true, 45);

        [UnityTest]
        public IEnumerator RotatedCoverStillAllowsFlankHits() => CheckShot(new Vector3(4, 1.35f, .64f), false, 45);

        [UnityTest]
        public IEnumerator ShotClearingTheEndHitsTheEnemy() => CheckShot(new Vector3(4, 1.35f, -.2f), false, 0, 1.2f);

        [UnityTest]
        public IEnumerator ObliqueShotCrossingTheFootprintHitsCover() => CheckShot(new Vector3(2, 1.35f, -4), true);

        private IEnumerator CheckShot(Vector3 from, bool protectedSide, float angle = 0, float x = 0)
        {
            CreateRoom(angle, x);
            yield return EnterCover();
            Shoot(from);
            yield return WaitForHit();
            Assert.AreEqual(1, m_hits);
            Assert.AreEqual(protectedSide ? 100 : 88, m_health.CurrentHealth);
            Assert.AreSame(protectedSide ? (Component)m_wall : m_body, m_lastHit.Collider);
            Assert.AreEqual(1.8f, m_body.height, .001f, "Crouching does not resize the character capsule.");
            Assert.AreEqual(1, m_entity.GetComponentsInChildren<CapsuleCollider>().Length);
        }

        [UnityTest]
        public IEnumerator ExposedEnemyCanBeHitOverLowCover()
        {
            CreateRoom();
            yield return EnterCover();
            m_movement.SetCoverExposure(true, Vector3.back);
            yield return new WaitForFixedUpdate();
            yield return null;
            Assert.IsTrue(m_movement.IsExposed);
            Assert.AreEqual(1.8f, m_body.height, .001f);
            Shoot(new Vector3(0, 1.35f, -4));
            yield return WaitForHit();
            Assert.AreEqual(88, m_health.CurrentHealth);
            Assert.AreSame(m_body, m_lastHit.Collider);
        }

        [UnityTest]
        public IEnumerator OutgoingShotFromExposedCoverPassesOverTheBarricade()
        {
            CreateRoom();
            yield return EnterCover();
            m_movement.SetCoverExposure(true, Vector3.back);
            yield return new WaitForFixedUpdate();
            yield return null;
            Projectile projectile = Shoot(new Vector3(0, 1.35f, .2f), Vector3.back, m_entity);
            projectile.Speed = 40;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(0, m_hits, $"Unexpected hit: {m_lastHit.Other}");
            Assert.IsTrue(projectile && projectile.enabled);
            Assert.Less(projectile.transform.position.z, Origin.z - .4f);
        }

        [UnityTest]
        public IEnumerator ExposedShooterCanPassThroughAnotherUnoccupiedLowCover()
        {
            CreateRoom();
            yield return EnterCover();
            m_movement.SetCoverExposure(true, Vector3.back);
            yield return new WaitForFixedUpdate();
            yield return null;
            Assert.IsTrue(m_movement.IsExposed);
            GameObject other = Track(new GameObject("Other low cover"));
            other.transform.position = Origin + new Vector3(0, .9f, -2);
            BoxCollider wall = other.AddComponent<BoxCollider>();
            wall.size = new Vector3(3.2f, 1.8f, .58f);
            other.AddComponent<Cover>();
            Physics.SyncTransforms();
            Projectile shot = Shoot(new Vector3(0, 1.35f, .2f), Vector3.back, m_entity);
            for (int i = 0; i < 4; i++) yield return new WaitForFixedUpdate();
            Assert.AreEqual(0, m_hits);
            Assert.IsTrue(shot && shot.enabled);
            Assert.Less(shot.transform.position.z, Origin.z - 3);
        }

        [UnityTest]
        public IEnumerator RealObstacleBeforeTheEnemyStopsTheShot()
        {
            CreateRoom();
            yield return EnterCover();
            GameObject obstacle = Track(new GameObject("Intervening wall"));
            obstacle.transform.position = Origin + new Vector3(0, 1, 2);
            BoxCollider blocker = obstacle.AddComponent<BoxCollider>();
            blocker.size = new Vector3(3, 2, .3f);
            Physics.SyncTransforms();
            Shoot(new Vector3(0, 1.35f, 4));
            yield return WaitForHit();
            Assert.AreEqual(100, m_health.CurrentHealth);
            Assert.AreSame(blocker, m_lastHit.Collider);
        }

        [UnityTest]
        public IEnumerator FrontAndRearShotsResolveIndependentlyInTheSameStep()
        {
            CreateRoom();
            yield return EnterCover();
            Shoot(new Vector3(0, 1.35f, -4));
            Shoot(new Vector3(0, 1.35f, 4));
            for (int i = 0; i < 30 && m_hits < 2; i++) yield return new WaitForFixedUpdate();
            Assert.AreEqual(2, m_hits);
            Assert.AreEqual(88, m_health.CurrentHealth);
        }

        [UnityTest]
        public IEnumerator UnoccupiedLowCoverLetsTheShotReachTheEnemy()
        {
            CreateRoom();
            Shoot(new Vector3(0, 1.35f, -4));
            yield return WaitForHit();
            Assert.AreEqual(88, m_health.CurrentHealth);
            Assert.AreSame(m_body, m_lastHit.Collider);
        }

        [UnityTest]
        public IEnumerator LeavingAndReenteringCoverKeepsOneUnchangedCapsule()
        {
            CreateRoom();
            yield return EnterCover();
            m_movement.LeaveCover();
            yield return new WaitForSeconds(.4f);
            Assert.AreEqual(1.8f, m_body.height, .001f);
            Assert.AreEqual(.9f, m_body.center.y, .001f);
            Assert.AreEqual(1, m_entity.GetComponentsInChildren<CapsuleCollider>().Length);
            Assert.AreEqual(1, m_cover.GetComponentsInChildren<Collider>().Length);
            yield return EnterCover();
            Assert.AreEqual(1, m_entity.GetComponentsInChildren<CapsuleCollider>().Length);
            Shoot(new Vector3(0, 1.35f, -4));
            yield return WaitForHit();
            Assert.AreEqual(100, m_health.CurrentHealth);
        }

        [UnityTest]
        public IEnumerator ShotFromTheUnprotectedSidePassesThroughLowCover()
        {
            CreateRoom();
            yield return EnterCover();
            Projectile shot = Shoot(new Vector3(1, 1.35f, 2), Vector3.back);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(0, m_hits);
            Assert.IsTrue(shot && shot.enabled);
            Assert.Less(shot.transform.position.z, Origin.z - 1);
        }

        [UnityTest]
        public IEnumerator LeavingCoverRemovesItsProtection()
        {
            CreateRoom();
            yield return EnterCover();
            m_movement.LeaveCover();
            yield return new WaitForSeconds(.4f);
            Shoot(new Vector3(0, 1.35f, -4));
            yield return WaitForHit();
            Assert.AreEqual(88, m_health.CurrentHealth);
            Assert.AreSame(m_body, m_lastHit.Collider);
        }

        [UnityTest]
        public IEnumerator UnoccupiedHighCoverStillBlocksShots()
        {
            CreateRoom();
            SetField(m_cover, "m_type", CoverType.High);
            Shoot(new Vector3(0, 1.35f, -4));
            yield return WaitForHit();
            Assert.AreEqual(100, m_health.CurrentHealth);
            Assert.AreSame(m_wall, m_lastHit.Collider);
        }

        private void CreateRoom(float angle = 0, float x = 0)
        {
            m_rotation = Quaternion.Euler(0, angle, 0);
            GameObject wall = Track(new GameObject("Planar low cover"));
            wall.transform.SetPositionAndRotation(Origin, m_rotation);
            m_wall = wall.AddComponent<BoxCollider>();
            m_wall.size = new Vector3(3.2f, 1.8f, .58f);
            m_wall.center = Vector3.up * .9f;
            m_cover = wall.AddComponent<Cover>();

            GameObject character = Track(new GameObject("Covered target"));
            character.SetActive(false);
            character.transform.SetPositionAndRotation(Origin + m_rotation * new Vector3(x, 0, .64f), m_rotation);
            Rigidbody body = character.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
            m_body = character.AddComponent<CapsuleCollider>();
            m_body.height = 1.8f;
            m_body.radius = .3f;
            m_body.center = Vector3.up * .9f;
            m_entity = character.AddComponent<Entity>();
            m_health = character.AddComponent<Health>();
            SetField(m_health, "m_entity", m_entity);
            SetField(m_health, "m_maxHealth", 100);
            SetField(m_health, "m_destroyEntityOnDeath", false);
            SetField(m_entity, "m_health", m_health);
            m_movement = character.AddComponent<CoverMovement>();
            SetField(m_movement, "m_entity", m_entity);
            SetField(m_movement, "m_coverLayers", (LayerMask)1);
            SetField(m_movement, "m_crouchHeight", 1.8f);
            SetField(m_entity, "m_movement", m_movement);
            character.SetActive(true);
            m_hits = 0;
            Physics.SyncTransforms();
        }

        private IEnumerator EnterCover()
        {
            m_movement.TakeCover();
            yield return new WaitForSeconds(.6f);
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(CoverState.Holding, m_movement.State);
            Assert.AreEqual(1, m_cover.GetComponentsInChildren<Collider>().Length, "Cover must use its existing collider.");
            Assert.AreEqual(1.8f, m_body.height, .001f);
        }

        private Projectile Shoot(Vector3 from, Vector3 direction = default, Entity owner = null)
        {
            Vector3 position = Origin + m_rotation * from;
            GameObject shot = Track(new GameObject("Planar test shot"));
            shot.transform.position = position;
            Rigidbody body = shot.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            shot.AddComponent<SphereCollider>().radius = .12f;
            Projectile projectile = shot.AddComponent<Projectile>();
            projectile.Damage = new Damage(12);
            projectile.Speed = 300;
            projectile.Range = 100;
            projectile.Owner = owner;
            projectile.OnHit += hit =>
            {
                m_lastHit = hit;
                m_hits++;
            };
            Vector3 aim = direction != Vector3.zero ? m_rotation * direction
                : Vector3.ProjectOnPlane(m_entity.transform.position - position, Vector3.up).normalized;
            projectile.Launch(position, aim);
            Physics.SyncTransforms();
            return projectile;
        }

        private IEnumerator WaitForHit()
        {
            for (int i = 0; i < 30 && m_hits == 0; i++) yield return new WaitForFixedUpdate();
            Assert.Greater(m_hits, 0, "Expected a projectile impact.");
        }

        private GameObject Track(GameObject obj)
        {
            m_objects.Add(obj);
            return obj;
        }

        private static void SetField(object target, string name, object value)
        {
            for (Type type = target.GetType(); type != null; type = type.BaseType)
            {
                FieldInfo field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
                if (field == null) continue;
                field.SetValue(target, value);
                return;
            }
            throw new MissingFieldException(target.GetType().Name, name);
        }
    }
}
