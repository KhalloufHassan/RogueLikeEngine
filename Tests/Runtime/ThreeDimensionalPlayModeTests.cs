using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using RogueLikeEngine.Input;
using RogueLikeEngine.Optimization.Pooling;
using RogueLikeEngine.Systems.Entities;
using RogueLikeEngine.Systems.Healths;
using RogueLikeEngine.Systems.Movements;
using RogueLikeEngine.Systems.Weapons;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace RogueLikeEngine.Tests.PlayMode
{
    public class ThreeDimensionalPlayModeTests
    {
        private readonly HashSet<Object> m_objects = new();

        [TearDown]
        public void TearDown()
        {
            foreach (Object obj in m_objects)
            {
                if (obj) Object.Destroy(obj);
            }

            m_objects.Clear();
        }

        private T Track<T>(T obj) where T : Object
        {
            m_objects.Add(obj);
            return obj;
        }

        private GameObject CreateInactive(string name)
        {
            GameObject go = Track(new GameObject(name));
            go.SetActive(false);
            return go;
        }

        private Movement CreateWalker(bool useGravity)
        {
            GameObject go = CreateInactive("Walker");
            go.AddComponent<CapsuleCollider>();
            go.AddComponent<Rigidbody>().useGravity = useGravity;
            Entity entity = go.AddComponent<Entity>();
            Movement movement = go.AddComponent<Movement>();
            SetField(entity, "m_movement", movement);
            SetField(movement, "m_entity", entity);
            go.SetActive(true);
            return movement;
        }

        [UnityTest]
        public IEnumerator Movement3D_WalksOnTheGround_AndKeepsGravity()
        {
            Movement movement = CreateWalker(useGravity: true);
            yield return null;

            movement.Move(Vector3.right);
            yield return new WaitForSeconds(0.3f);

            Assert.Greater(movement.Position.x, 0.5f, "Should have walked along +X");
            Assert.AreEqual(0f, movement.Position.z, 1e-3f);
            Assert.Less(movement.Velocity.y, -0.5f, "Walking must not cancel gravity");
        }

        [UnityTest]
        public IEnumerator Movement3D_IgnoresTheVerticalPartOfADirection()
        {
            Movement movement = CreateWalker(useGravity: false);
            yield return null;

            movement.Move(new Vector3(1, 1, 0));

            Assert.AreEqual(0f, movement.MovementDirection.y);
            Assert.AreEqual(1f, movement.MovementDirection.magnitude, 1e-4f);
        }

        [UnityTest]
        public IEnumerator Projectile3D_HitsAHealthEntity_AndReturnsToPool()
        {
            GameObject target = CreateInactive("Target");
            target.transform.position = new Vector3(0, 0, 3);
            target.AddComponent<BoxCollider>();
            Entity targetEntity = target.AddComponent<Entity>();
            Health health = target.AddComponent<Health>();
            SetField(targetEntity, "m_health", health);
            SetField(health, "m_entity", targetEntity);
            SetField(health, "m_maxHealth", 100);
            SetField(health, "m_destroyEntityOnDeath", false);
            target.SetActive(true);

            GameObject template = CreateInactive("ProjectileTemplate");
            template.AddComponent<Rigidbody>().useGravity = false;
            template.AddComponent<SphereCollider>().radius = 0.1f;
            ProjectilesPool pool = Track(ScriptableObject.CreateInstance<ProjectilesPool>());
            pool.prefab = template.AddComponent<Projectile>();

            Projectile projectile = pool.Request();
            Track(projectile.transform.parent.gameObject);
            projectile.transform.position = Vector3.zero;
            projectile.Range = 1000;
            projectile.Speed = 10;
            projectile.Damage = new Damage(10);
            projectile.SetDirection(Vector3.forward);

            float timeout = Time.time + 2f;
            while (projectile.gameObject.activeSelf && Time.time < timeout)
                yield return new WaitForFixedUpdate();

            Assert.AreEqual(90, health.CurrentHealth);
            Assert.IsTrue(projectile, "The projectile must go back to its pool, not be destroyed");
            Assert.IsFalse(projectile.gameObject.activeSelf);
        }

        [UnityTest]
        public IEnumerator GunPoint3D_OrbitsAndFacesTheAim_AtItsHeight()
        {
            GameObject owner = CreateInactive("Owner");
            owner.AddComponent<Rigidbody>().useGravity = false;
            Entity entity = owner.AddComponent<Entity>();
            WeaponsSystem weapons = owner.AddComponent<WeaponsSystem>();
            SetField(weapons, "m_entity", entity);

            GameObject gun = new("GunPoint");
            gun.transform.SetParent(owner.transform);
            gun.transform.localPosition = new Vector3(0, 1, 0);
            GunPoint gunPoint = gun.AddComponent<GunPoint>();
            SetField(gunPoint, "weaponsSystem", weapons);
            SetField(gunPoint, "shootingTransform", gun.transform);
            SetField(gunPoint, "weaponData", Track(ScriptableObject.CreateInstance<WeaponData>()));
            SetField(gunPoint, "m_distanceFromOwner", 0.75f);
            owner.SetActive(true);
            yield return null;

            weapons.Aim(Vector3.left);
            yield return null;

            Vector3 expected = owner.transform.position + Vector3.left * 0.75f + Vector3.up;
            Assert.That(Vector3.Distance(expected, gun.transform.position), Is.LessThan(1e-3f));
            Assert.That(Vector3.Angle(Vector3.left, gun.transform.forward), Is.LessThan(0.1f));
        }

        [Test]
        public void MouseAim3D_ReturnsTheGroundDirection_UnderATopDownCamera()
        {
            GameObject cameraObject = Track(new GameObject("Camera"));
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            cameraObject.transform.SetPositionAndRotation(new Vector3(0, 10, 0), Quaternion.Euler(90, 0, 0));

            GameObject player = Track(new GameObject("Player"));
            player.tag = "Player";
            player.AddComponent<Rigidbody>().useGravity = false;

            Vector2 mouse = camera.WorldToScreenPoint(new Vector3(3, 0, 0));
            Vector2 aim = new MousePositionToAimProcessor().Process(mouse, null);

            Assert.That(Vector2.Distance(new Vector2(1, 0), aim), Is.LessThan(1e-3f));
        }

        private static void SetField(object target, string fieldName, object value)
        {
            for (Type type = target.GetType(); type != null; type = type.BaseType)
            {
                FieldInfo field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field == null) continue;
                field.SetValue(target, value);
                return;
            }

            throw new MissingFieldException(target.GetType().Name, fieldName);
        }
    }
}
