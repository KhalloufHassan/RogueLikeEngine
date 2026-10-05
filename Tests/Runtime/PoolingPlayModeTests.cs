using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using RogueLikeEngine.Optimization.Pooling;
using RogueLikeEngine.Systems.Entities;
using RogueLikeEngine.Systems.Healths;
using RogueLikeEngine.Systems.Movements;
using RogueLikeEngine.Systems.Weapons;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace RogueLikeEngine.Tests.PlayMode
{
    public class PoolingPlayModeTests
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

        /// <summary>Inactive template, so it doesn't run in the scene while the pool instantiates copies of it.</summary>
        private GameObject CreateTemplate(string name)
        {
            GameObject template = Track(new GameObject(name));
            template.SetActive(false);
            return template;
        }

        private Projectile CreateProjectileTemplate()
        {
            GameObject go = CreateTemplate("ProjectileTemplate");
            Rigidbody2D body = go.AddComponent<Rigidbody2D>();
            body.gravityScale = 0;
            go.AddComponent<CircleCollider2D>().radius = 0.1f;
            Projectile projectile = go.AddComponent<Projectile>();
            Movement movement = go.AddComponent<Movement>();
            SetField(projectile, "m_movement", movement);
            SetField(movement, "m_entity", projectile);
            SetField(movement, "m_rigidbody", body);
            return projectile;
        }

        private ProjectilesPool CreateProjectilesPool()
        {
            ProjectilesPool pool = Track(ScriptableObject.CreateInstance<ProjectilesPool>());
            pool.prefab = CreateProjectileTemplate();
            return pool;
        }

        private Projectile Request(ProjectilesPool pool)
        {
            Projectile projectile = pool.Request();
            Track(projectile.transform.parent.gameObject); // the pool root, kept alive by DontDestroyOnLoad
            projectile.Range = 1000;
            return projectile;
        }

        [UnityTest]
        public IEnumerator ReturnedProjectile_IsReusedWithResetState()
        {
            ProjectilesPool pool = CreateProjectilesPool();
            Projectile first = Request(pool);
            first.SetDirection(Vector2.right);
            yield return new WaitForSeconds(0.1f);
            Assert.Greater(first.Movement.TraveledDistance, 0f);

            first.DestroyEntity();
            yield return null;

            Assert.IsTrue(first, "A pooled projectile must not be destroyed");
            Assert.IsFalse(first.gameObject.activeSelf);

            Projectile second = Request(pool);

            Assert.AreSame(first, second);
            Assert.IsTrue(second.gameObject.activeSelf);
            Assert.AreEqual(0f, second.Movement.TraveledDistance);
        }

        [UnityTest]
        public IEnumerator ProjectileOutOfRange_ReturnsToPool()
        {
            ProjectilesPool pool = CreateProjectilesPool();
            Projectile projectile = Request(pool);
            projectile.Range = 0.2f;
            projectile.SetDirection(Vector2.right);

            yield return new WaitForSeconds(0.5f);

            Assert.IsTrue(projectile);
            Assert.IsFalse(projectile.gameObject.activeSelf);
            Assert.IsTrue(projectile.IsDisposed);
        }

        [UnityTest]
        public IEnumerator ProjectileHittingHealth_DamagesItAndReturnsToPool()
        {
            GameObject target = CreateTemplate("Target");
            target.transform.position = new Vector3(2, 0, 0);
            target.AddComponent<BoxCollider2D>();
            Entity targetEntity = target.AddComponent<Entity>();
            Health health = target.AddComponent<Health>();
            SetField(health, "m_entity", targetEntity);
            SetField(health, "m_maxHealth", 100);
            SetField(health, "m_destroyEntityOnDeath", false);
            target.SetActive(true);

            Projectile projectile = Request(CreateProjectilesPool());
            projectile.transform.position = Vector3.zero;
            projectile.Damage = new Damage(10);
            projectile.SetDirection(Vector2.right);

            float timeout = Time.time + 2f;
            while (health.CurrentHealth == 100 && Time.time < timeout)
                yield return new WaitForFixedUpdate();

            Assert.AreEqual(90, health.CurrentHealth);
            Assert.IsTrue(projectile, "The projectile must go back to its pool, not be destroyed");
            Assert.IsFalse(projectile.gameObject.activeSelf);
        }

        [UnityTest]
        public IEnumerator DamagePopup_ReturnsToPoolAfterItsAnimation()
        {
            GameObject go = CreateTemplate("PopupTemplate");
            TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
            FloatingDamageUI template = go.AddComponent<FloatingDamageUI>();
            SetField(template, "text", text);
            SetField(template, "floatingDistance", 1f);
            SetField(template, "floatingDuration", 0.2f);
            SetField(template, "scalePunch", 0.5f);

            FloatingDamagePool pool = Track(ScriptableObject.CreateInstance<FloatingDamagePool>());
            pool.prefab = template;

            FloatingDamageUI popup = pool.Request();
            Track(popup.transform.parent.gameObject);
            Vector3 baseScale = popup.transform.localScale;
            popup.Show(new Damage(5), Vector2.zero);
            Assert.AreEqual("5", popup.GetComponent<TextMeshProUGUI>().text);

            yield return new WaitForSeconds(0.5f);

            Assert.IsFalse(popup.gameObject.activeSelf, "The popup should have returned to its pool");
            Assert.IsTrue(popup.IsDisposed);
            Assert.AreEqual(baseScale, popup.transform.localScale, "The scale punch must be reset");
            Assert.AreSame(popup, pool.Request(), "The returned popup should be reused");
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
