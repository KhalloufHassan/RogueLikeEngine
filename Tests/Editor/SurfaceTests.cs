using NUnit.Framework;
using RogueLikeEngine.Surfaces;
using UnityEngine;

namespace RogueLikeEngine.Tests
{
    public class SurfaceTests
    {
        private TestObjects m_objects;
        private SurfaceDefinition m_metal;
        private SurfaceDefinition m_flesh;

        [SetUp]
        public void SetUp()
        {
            m_objects = new TestObjects();
            m_metal = m_objects.CreateAsset<SurfaceDefinition>();
            m_flesh = m_objects.CreateAsset<SurfaceDefinition>();
        }

        [TearDown]
        public void TearDown() => m_objects.Dispose();

        private static SurfaceEffect Entry(SurfaceDefinition surface)
        {
            SurfaceEffect effect = new();
            TestReflection.SetField(effect, "m_surface", surface);
            return effect;
        }

        private SurfaceEffects Table(SurfaceEffects fallback, params SurfaceEffect[] effects)
        {
            SurfaceEffects table = m_objects.CreateAsset<SurfaceEffects>();
            TestReflection.SetField(table, "m_effects", effects);
            TestReflection.SetField(table, "m_fallback", fallback);
            return table;
        }

        [Test]
        public void Get_ReturnsExactMatch_OrDefaultForUnlistedSurfaces()
        {
            SurfaceEffect metal = Entry(m_metal), fallbackDefault = Entry(null);
            SurfaceEffects table = Table(null, fallbackDefault, metal);
            Assert.AreSame(metal, table.Get(m_metal));
            Assert.AreSame(fallbackDefault, table.Get(m_flesh));
            Assert.AreSame(fallbackDefault, table.Get(null));
        }

        [Test]
        public void Get_SearchesFallbackForExactMatchBeforeAnyDefault()
        {
            SurfaceEffect sharedMetal = Entry(m_metal), sharedDefault = Entry(null), ownFlesh = Entry(m_flesh), ownDefault = Entry(null);
            SurfaceEffects shared = Table(null, sharedDefault, sharedMetal);
            SurfaceEffects table = Table(shared, ownFlesh, ownDefault);
            Assert.AreSame(ownFlesh, table.Get(m_flesh));
            Assert.AreSame(sharedMetal, table.Get(m_metal));
            Assert.AreSame(ownDefault, table.Get(null));
        }

        [Test]
        public void Get_UsesFallbackDefault_AndReturnsNullWithoutAny()
        {
            SurfaceEffect sharedDefault = Entry(null);
            Assert.AreSame(sharedDefault, Table(Table(null, sharedDefault), Entry(m_flesh)).Get(m_metal));
            Assert.IsNull(Table(null, Entry(m_flesh)).Get(m_metal));
        }

        [Test]
        public void Get_StopsOnFallbackCycles()
        {
            SurfaceEffects first = Table(null, Entry(m_flesh));
            SurfaceEffects second = Table(first, Entry(m_flesh));
            TestReflection.SetField(first, "m_fallback", second);
            Assert.IsNull(first.Get(m_metal));
        }

        [Test]
        public void SurfaceOf_FindsClosestParentSurface_OrNull()
        {
            Surface surface = m_objects.CreateComponent<Surface>();
            surface.Definition = m_metal;
            BoxCollider child = m_objects.CreateComponent<BoxCollider>();
            child.transform.SetParent(surface.transform);
            Assert.AreSame(m_metal, Surface.Of(child));

            surface.Definition = m_flesh;
            Assert.AreSame(m_flesh, Surface.Of(child), "the surface can change at runtime");
            Assert.IsNull(Surface.Of(m_objects.CreateComponent<BoxCollider>()));
            Assert.IsNull(Surface.Of(null));
        }
    }
}
