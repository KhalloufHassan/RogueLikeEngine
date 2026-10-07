using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using RogueLikeEngine.Systems.Entities;
using UnityEngine;
using UnityEngine.TestTools;

namespace RogueLikeEngine.Tests.PlayMode
{
    public class HitRoutingPlayModeTests
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

        private HitCountingEffect AddHitCounter(Entity entity)
        {
            HitCountingEffect copy = null;
            entity.OnEffectAdded += e => copy = Track((HitCountingEffect)e);
            entity.AddEffect(Track(ScriptableObject.CreateInstance<HitCountingEffect>()));
            return copy;
        }

        private static IEnumerator WaitForHit(HitCountingEffect effect)
        {
            float timeout = Time.time + 2f;
            while (effect.hits == 0 && Time.time < timeout)
                yield return new WaitForFixedUpdate();
        }

        [UnityTest]
        public IEnumerator Entity_3DCollision_ReachesItsHitEffects()
        {
            GameObject wall = Track(new GameObject("Wall"));
            wall.transform.position = new Vector3(2, 0, 0);
            wall.AddComponent<BoxCollider>();

            GameObject mover = Track(new GameObject("Mover"));
            mover.AddComponent<BoxCollider>();
            Rigidbody body = mover.AddComponent<Rigidbody>();
            body.useGravity = false;
            Entity entity = mover.AddComponent<Entity>();
            HitCountingEffect effect = AddHitCounter(entity);
            body.linearVelocity = new Vector3(10, 0, 0);

            yield return WaitForHit(effect);

            Assert.AreEqual(1, effect.hits);
            Assert.AreSame(wall, effect.lastHit.Other);
            Assert.IsFalse(effect.lastHit.IsTrigger);
            Assert.IsInstanceOf<Collider>(effect.lastHit.Collider);
        }

        [UnityTest]
        public IEnumerator Entity_2DTrigger_ReachesItsHitEffectsAsATrigger()
        {
            GameObject zone = Track(new GameObject("Zone"));
            zone.transform.position = new Vector3(2, 0, 0);
            zone.AddComponent<BoxCollider2D>().isTrigger = true;

            GameObject mover = Track(new GameObject("Mover"));
            mover.AddComponent<BoxCollider2D>();
            Rigidbody2D body = mover.AddComponent<Rigidbody2D>();
            body.gravityScale = 0;
            Entity entity = mover.AddComponent<Entity>();
            HitCountingEffect effect = AddHitCounter(entity);
            body.linearVelocity = new Vector2(10, 0);

            yield return WaitForHit(effect);

            Assert.AreEqual(1, effect.hits);
            Assert.AreSame(zone, effect.lastHit.Other);
            Assert.IsTrue(effect.lastHit.IsTrigger);
            Assert.IsInstanceOf<Collider2D>(effect.lastHit.Collider);
        }
    }
}
