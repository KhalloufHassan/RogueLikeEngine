using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using RogueLikeEngine.Vfx;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;
using Object = UnityEngine.Object;

namespace RogueLikeEngine.Tests.PlayMode
{
    public class VfxManagerPlayModeTests
    {
        private readonly List<Object> m_objects = new();
        private VfxManager m_manager;

        [SetUp]
        public void SetUp() => m_manager = Track(ScriptableObject.CreateInstance<VfxManager>());

        [TearDown]
        public void TearDown()
        {
            DestroyPools();
            foreach (Object obj in m_objects)
                if (obj) Object.Destroy(obj);
            m_objects.Clear();
        }

        private T Track<T>(T obj) where T : Object
        {
            m_objects.Add(obj);
            return obj;
        }

        /// <summary>Inactive particle effect template, emitting for <paramref name="lifetime"/> seconds or looping.</summary>
        internal static PooledVfx CreateEffect(string name, float lifetime, bool loop = false)
        {
            GameObject template = new(name);
            template.SetActive(false);
            ParticleSystem particleSystem = template.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particleSystem.main;
            main.duration = lifetime;
            main.startLifetime = lifetime;
            main.loop = loop;
            return template.AddComponent<PooledVfx>();
        }

        internal static PooledVfx[] ActiveEffects => Object.FindObjectsByType<PooledVfx>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        internal static void DestroyPools()
        {
            foreach (PooledVfx effect in Object.FindObjectsByType<PooledVfx>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (effect.transform.parent) Object.Destroy(effect.transform.parent.gameObject);
        }

        private PooledVfx Effect(float lifetime, bool loop = false) => Track(CreateEffect("TestEffect", lifetime, loop));

        [UnityTest]
        public IEnumerator OneShot_ReturnsToPoolWhenFinished()
        {
            VfxHandle handle = m_manager.Play(Effect(0.1f), new Vector3(1, 2, 3), Quaternion.identity);
            Assert.IsTrue(handle.IsActive);
            Assert.AreEqual(1, ActiveEffects.Length);
            Assert.AreEqual(new Vector3(1, 2, 3), ActiveEffects[0].transform.position);
            yield return new WaitForSeconds(0.6f);
            Assert.IsFalse(handle.IsActive);
            Assert.AreEqual(0, ActiveEffects.Length);
        }

        [UnityTest]
        public IEnumerator Looping_PlaysUntilStopped_ThenLetsParticlesFinish()
        {
            VfxHandle handle = m_manager.Play(Effect(0.1f, loop: true), Vector3.zero, Quaternion.identity);
            yield return new WaitForSeconds(0.4f);
            Assert.IsTrue(handle.IsActive);
            handle.Stop();
            Assert.IsFalse(handle.IsActive);
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(0, ActiveEffects.Length);
        }

        [UnityTest]
        public IEnumerator StaleHandle_DoesNotStopEffectReusingItsInstance()
        {
            PooledVfx prefab = Effect(0.1f, loop: true);
            VfxHandle first = m_manager.Play(prefab, Vector3.zero, Quaternion.identity);
            first.Stop();
            yield return new WaitForSeconds(0.5f);

            VfxHandle second = m_manager.Play(prefab, Vector3.zero, Quaternion.identity);
            Assert.AreEqual(1, ActiveEffects[0].transform.parent.childCount, "the instance should be reused");
            first.Stop();
            Assert.IsTrue(second.IsActive);
        }

        [UnityTest]
        public IEnumerator FollowsTarget()
        {
            Transform target = Track(new GameObject("Target")).transform;
            m_manager.Play(Effect(0.1f, loop: true), target);
            target.SetPositionAndRotation(new Vector3(4, 0, 0), Quaternion.Euler(0, 90, 0));
            yield return null;
            Assert.AreEqual(new Vector3(4, 0, 0), ActiveEffects[0].transform.position);
            Assert.AreEqual(target.rotation, ActiveEffects[0].transform.rotation);
        }

        [UnityTest]
        public IEnumerator EachPrefab_PlaysFromItsOwnPool()
        {
            m_manager.Play(Track(CreateEffect("First", 1)), Vector3.zero, Quaternion.identity);
            m_manager.Play(Track(CreateEffect("Second", 1)), Vector3.zero, Quaternion.identity);
            yield return null;
            Assert.AreEqual(2, ActiveEffects.Length);
            Assert.AreNotSame(ActiveEffects[0].transform.parent, ActiveEffects[1].transform.parent);
        }

        [UnityTest]
        public IEnumerator Replaying_DoesNotAllocate()
        {
            PooledVfx prefab = Effect(0.1f);
            m_manager.Play(prefab, Vector3.zero, Quaternion.identity).Stop();
            yield return new WaitForSeconds(0.5f);
            Assert.That(() => m_manager.Play(prefab, Vector3.zero, Quaternion.identity).Stop(), Is.Not.AllocatingGCMemory());
        }
    }
}
