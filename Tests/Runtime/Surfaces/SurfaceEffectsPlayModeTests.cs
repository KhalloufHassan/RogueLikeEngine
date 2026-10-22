using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using RogueLikeEngine.Surfaces;
using RogueLikeEngine.Systems.Weapons;
using RogueLikeEngine.Vfx;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;
using Object = UnityEngine.Object;

namespace RogueLikeEngine.Tests.PlayMode
{
    public class SurfaceEffectsPlayModeTests
    {
        private readonly List<Object> m_objects = new();
        private SurfaceDefinition m_metal;
        private SurfaceEffects m_effects;

        [SetUp]
        public void SetUp()
        {
            m_metal = Track(ScriptableObject.CreateInstance<SurfaceDefinition>());
            m_effects = Track(ScriptableObject.CreateInstance<SurfaceEffects>());
            SetField(m_effects, "m_vfxManager", Track(ScriptableObject.CreateInstance<VfxManager>()));
            SetField(m_effects, "m_effects", new[]
            {
                Entry(m_metal, "MetalImpact", SurfaceEffectAlignment.SurfaceNormal),
                Entry(null, "DefaultImpact", SurfaceEffectAlignment.AgainstDirection)
            });
        }

        [TearDown]
        public void TearDown()
        {
            VfxManagerPlayModeTests.DestroyPools();
            foreach (Object obj in m_objects)
                if (obj) Object.Destroy(obj);
            m_objects.Clear();
        }

        private T Track<T>(T obj) where T : Object
        {
            m_objects.Add(obj);
            return obj;
        }

        private SurfaceEffect Entry(SurfaceDefinition surface, string vfxName, SurfaceEffectAlignment alignment)
        {
            SurfaceEffect effect = new();
            SetField(effect, "m_surface", surface);
            SetField(effect, "m_vfx", Track(VfxManagerPlayModeTests.CreateEffect(vfxName, 1)));
            SetField(effect, "m_alignment", alignment);
            return effect;
        }

        private static void SetField(object target, string name, object value) =>
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

        private BoxCollider CreateWall(SurfaceDefinition surface)
        {
            BoxCollider wall = Track(GameObject.CreatePrimitive(PrimitiveType.Cube)).GetComponent<BoxCollider>();
            wall.transform.position = new Vector3(2, 0, 0);
            if (surface) wall.gameObject.AddComponent<Surface>().Definition = surface;
            return wall;
        }

        private void LaunchProjectile()
        {
            GameObject go = Track(new GameObject("Projectile"));
            Rigidbody body = go.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            go.AddComponent<SphereCollider>().radius = 0.1f;
            Projectile projectile = go.AddComponent<Projectile>();
            SetField(projectile, "m_impactEffects", m_effects);
            projectile.Range = 100;
            projectile.Speed = 20;
            projectile.Launch(Vector3.zero, Vector3.right);
        }

        private static IEnumerator WaitForEffect()
        {
            for (float time = 0; time < 1 && VfxManagerPlayModeTests.ActiveEffects.Length == 0; time += Time.deltaTime)
                yield return null;
        }

        [UnityTest]
        public IEnumerator ProjectileHit_PlaysTheHitSurfacesEffect_AtTheHitPoint()
        {
            CreateWall(m_metal);
            LaunchProjectile();
            yield return WaitForEffect();
            PooledVfx[] effects = VfxManagerPlayModeTests.ActiveEffects;
            Assert.AreEqual(1, effects.Length);
            StringAssert.StartsWith("MetalImpact", effects[0].name);
            Assert.AreEqual(1.5f, effects[0].transform.position.x, 0.05f, "on the wall's face");
            Assert.Greater(Vector3.Dot(effects[0].transform.forward, Vector3.left), 0.99f, "along the surface normal");
        }

        [UnityTest]
        public IEnumerator ProjectileHit_OnObjectWithoutSurface_PlaysDefault_AgainstDirection()
        {
            CreateWall(null);
            LaunchProjectile();
            yield return WaitForEffect();
            PooledVfx[] effects = VfxManagerPlayModeTests.ActiveEffects;
            Assert.AreEqual(1, effects.Length);
            StringAssert.StartsWith("DefaultImpact", effects[0].name);
            Assert.Greater(Vector3.Dot(effects[0].transform.forward, Vector3.left), 0.99f, "back toward the shooter");
        }

        [UnityTest]
        public IEnumerator PlayingSurfaceEffects_DoesNotAllocate()
        {
            BoxCollider wall = CreateWall(m_metal);
            m_effects.Play(Surface.Of(wall), Vector3.zero, Vector3.left, Vector3.right);
            for (float time = 0; time < 3 && VfxManagerPlayModeTests.ActiveEffects.Length > 0; time += Time.deltaTime)
                yield return null;
            Assert.AreEqual(0, VfxManagerPlayModeTests.ActiveEffects.Length, "the warm-up effect should be back in its pool");
            Assert.That(() => m_effects.Play(Surface.Of(wall), Vector3.zero, Vector3.left, Vector3.right), Is.Not.AllocatingGCMemory());
        }
    }
}
