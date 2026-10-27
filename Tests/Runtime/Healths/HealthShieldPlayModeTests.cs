using System.Collections;
using System.Reflection;
using NUnit.Framework;
using RogueLikeEngine.Systems.Entities;
using RogueLikeEngine.Systems.Healths;
using RogueLikeEngine.Systems.Weapons;
using UnityEngine;
using UnityEngine.TestTools;

namespace RogueLikeEngine.Tests.PlayMode
{
    public class HealthShieldPlayModeTests
    {
        private GameObject m_owner;
        private Health m_health;

        [SetUp]
        public void SetUp()
        {
            m_owner = new GameObject("Shielded");
            m_owner.SetActive(false);
            Entity entity = m_owner.AddComponent<Entity>();
            m_health = m_owner.AddComponent<Health>();
            SetField(typeof(EntitySystem), m_health, "m_entity", entity);
            SetField(typeof(Entity), entity, "m_health", m_health);
            SetField(typeof(Health), m_health, "m_maxHealth", 100);
            SetField(typeof(Health), m_health, "m_destroyEntityOnDeath", false);
            SetField(typeof(Health), m_health, "m_maxShield", 20f);
            SetField(typeof(Health), m_health, "m_shieldRechargeDelay", .2f);
            SetField(typeof(Health), m_health, "m_shieldRechargeRate", 100f);
            m_owner.SetActive(true);
        }

        [TearDown]
        public void TearDown() => Object.Destroy(m_owner);

        [UnityTest]
        public IEnumerator Shield_RechargesOnlyAfterTheDelaySinceTheLastHit()
        {
            m_health.TakeDamage(new Damage(20));
            yield return new WaitForSeconds(.1f);
            Assert.AreEqual(0, m_health.CurrentShield);

            m_health.TakeDamage(new Damage(5));
            yield return new WaitForSeconds(.15f);
            Assert.AreEqual(0, m_health.CurrentShield);
            Assert.AreEqual(95, m_health.CurrentHealth);

            yield return new WaitForSeconds(.4f);
            Assert.AreEqual(20, m_health.CurrentShield);
            Assert.AreEqual(95, m_health.CurrentHealth);
        }

        private static void SetField(System.Type type, object target, string name, object value) =>
            type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }
}
