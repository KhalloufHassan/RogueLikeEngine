using System.Reflection;
using NUnit.Framework;
using RogueLikeEngine.Audio;
using RogueLikeEngine.Surfaces;
using RogueLikeEngine.Systems.Movements;
using UnityEngine;

namespace RogueLikeEngine.Tests
{
    public class MovementFootstepsTests
    {
        private TestObjects m_objects;
        private MovementFootsteps m_footsteps;
        private IBody m_body;
        private Rigidbody m_body3D;
        private Rigidbody2D m_body2D;
        private GameObject m_floor;
        private Surface m_surface;
        private FootstepCountingSound m_default;
        private FootstepCountingSound m_metal;

        [SetUp]
        public void SetUp()
        {
            m_objects = new TestObjects();
            m_footsteps = new MovementFootsteps();
            TestReflection.SetField(m_footsteps, "m_enabled", true);
            TestReflection.SetField(m_footsteps, "m_floorLayers", (LayerMask)(1 << 30));
            TestReflection.SetField(m_footsteps, "m_walkDistance", .15f);
            TestReflection.SetField(m_footsteps, "m_runDistance", .15f);
            TestReflection.SetField(m_footsteps, "m_coverDistance", .15f);
            m_default = new FootstepCountingSound();
            m_metal = new FootstepCountingSound();
            m_surface = m_objects.CreateComponent<Surface>();
            m_surface.Definition = m_objects.CreateAsset<SurfaceDefinition>();
            SurfaceEffect metal = new();
            TestReflection.SetField(metal, "m_surface", m_surface.Definition);
            TestReflection.SetField(metal, "m_sound", m_metal);
            TestReflection.SetField(m_footsteps, "m_walk", Table(m_default, metal));
        }

        [TearDown]
        public void TearDown() => m_objects.Dispose();

        private SurfaceEffects Table(FootstepCountingSound sound, params SurfaceEffect[] extra)
        {
            SurfaceEffects table = m_objects.CreateAsset<SurfaceEffects>();
            TestReflection.SetField(table, "m_soundManager", m_objects.CreateAsset<SoundManager>());
            SurfaceEffect fallback = new();
            TestReflection.SetField(fallback, "m_sound", sound);
            SurfaceEffect[] entries = new SurfaceEffect[extra.Length + 1];
            extra.CopyTo(entries, 0);
            entries[extra.Length] = fallback;
            TestReflection.SetField(table, "m_effects", entries);
            return table;
        }

        private void CreateGround(bool use2D)
        {
            Vector3 origin = new(1000, 1000, 0);
            if (use2D)
            {
                m_body2D = m_objects.CreateComponent<Rigidbody2D>();
                m_body2D.gravityScale = 0;
                m_body2D.position = origin;
                m_body2D.linearVelocity = Vector2.right;
                m_body = new Body2D(m_body2D);
                BoxCollider2D floor = m_objects.CreateComponent<BoxCollider2D>();
                floor.size = new Vector2(20, 20);
                floor.isTrigger = true;
                m_floor = floor.gameObject;
                m_floor.transform.position = origin;
            }
            else
            {
                m_body3D = m_objects.CreateComponent<Rigidbody>();
                m_body3D.useGravity = false;
                m_body3D.position = origin;
                m_body3D.linearVelocity = Vector3.right;
                m_body = new Body3D(m_body3D);
                BoxCollider floor = m_objects.CreateComponent<BoxCollider>();
                floor.size = new Vector3(20, .1f, 20);
                m_floor = floor.gameObject;
                m_floor.transform.position = origin - Vector3.up * .05f;
            }
            m_floor.layer = 30;
            m_floor.transform.SetParent(m_surface.transform, true);
            Physics.SyncTransforms();
            Physics2D.SyncTransforms();
            Tick(0);
        }

        private void Tick(float travel = .1f, bool active = true, bool cover = false, float deltaTime = .1f)
        {
            if (m_body2D) m_body2D.position += Vector2.right * travel;
            else m_body3D.position += Vector3.right * travel;
            typeof(MovementFootsteps).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(m_footsteps, new object[] { m_body, active, cover, deltaTime });
        }

        [TestCase(false)]
        [TestCase(true)]
        public void StepsUseActualTravelAndClosestParentSurface_InBothPhysicsSystems(bool use2D)
        {
            CreateGround(use2D);
            for (int i = 0; i < 10; i++) Tick(0);
            Assert.AreEqual(0, m_metal.Plays, "velocity alone must not play steps against a wall");
            Tick();
            Assert.AreEqual(0, m_metal.Plays);
            Tick();
            Assert.AreEqual(1, m_metal.Plays);
            Assert.AreEqual(0, m_default.Plays);
            m_surface.Definition = null;
            Tick();
            Tick();
            Assert.Greater(m_default.Plays, 0, "unmarked ground uses the table default");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void MissingGroundOrExcludedLayerProducesNoStep(bool use2D)
        {
            CreateGround(use2D);
            m_floor.layer = 29;
            for (int i = 0; i < 5; i++) Tick();
            Assert.AreEqual(0, m_metal.Plays);
            m_floor.layer = 30;
            m_floor.SetActive(false);
            for (int i = 0; i < 5; i++) Tick();
            Assert.AreEqual(0, m_metal.Plays);
        }

        [Test]
        public void InactivePausedAndTeleportedMovementDoesNotEmitCatchUpSteps()
        {
            CreateGround(false);
            Tick();
            Tick(1, false);
            Tick(0, deltaTime: 0);
            Tick(0);
            Tick(5);
            Tick(.05f);
            Assert.AreEqual(0, m_metal.Plays);
            Tick(.11f);
            Assert.AreEqual(1, m_metal.Plays);
        }

        [Test]
        public void RunAndCoverChooseTheirOwnTables_AndFallBackToWalk()
        {
            CreateGround(false);
            FootstepCountingSound run = new(), cover = new();
            TestReflection.SetField(m_footsteps, "m_run", Table(run));
            TestReflection.SetField(m_footsteps, "m_cover", Table(cover));
            TestReflection.SetField(m_footsteps, "m_runSpeed", .5f);
            Tick();
            Tick();
            Assert.AreEqual(1, run.Plays);
            Tick(cover: true);
            Tick(cover: true);
            Assert.Greater(cover.Plays, 0);
            Assert.AreEqual(1, run.Plays);
            TestReflection.SetField(m_footsteps, "m_cover", null);
            Tick(cover: true);
            Tick(cover: true);
            Assert.Greater(m_metal.Plays, 0);
        }

        [Test]
        public void FootstepsAreOptIn()
        {
            CreateGround(false);
            TestReflection.SetField(m_footsteps, "m_enabled", false);
            for (int i = 0; i < 10; i++) Tick();
            Assert.AreEqual(0, m_metal.Plays);
        }
    }
}
