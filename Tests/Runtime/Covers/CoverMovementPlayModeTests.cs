using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using RogueLikeEngine.Input;
using RogueLikeEngine.Systems.Entities;
using RogueLikeEngine.Systems.Movements;
using RogueLikeEngine.Systems.Weapons;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace RogueLikeEngine.Tests.PlayMode
{
    public class CoverMovementPlayModeTests
    {
        private const int CoverLayer = 31;
        private const string CoverBinding = "<Keyboard>/space";
        private static readonly Vector3 Origin = new(500, 0, 500);
        private GameObject m_character;
        private GameObject m_wall;
        private GameObject m_ceiling;
        private Entity m_entity;
        private CoverMovement m_movement;
        private CapsuleCollider m_capsule;
        private Rigidbody m_body;
        private Cover m_cover;

        [SetUp]
        public void SetUp()
        {
            m_character = new GameObject("Cover movement test");
            m_character.SetActive(false);
            m_character.transform.position = Origin;
            m_body = m_character.AddComponent<Rigidbody>();
            m_body.useGravity = false;
            m_body.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
            m_capsule = m_character.AddComponent<CapsuleCollider>();
            m_capsule.height = 1.8f;
            m_capsule.radius = .3f;
            m_capsule.center = Vector3.up * .9f;
            m_entity = m_character.AddComponent<Entity>();
            m_movement = m_character.AddComponent<CoverMovement>();
            SetField(typeof(Entity), m_entity, "m_movement", m_movement);
            SetField(typeof(EntitySystem), m_movement, "m_entity", m_entity);
            SetField(typeof(Movement), m_movement, "m_baseSpeed", 6f);
            SetField(typeof(CoverMovement), m_movement, "m_coverLayers", (LayerMask)(1 << CoverLayer));
            m_character.SetActive(true);

            m_wall = new GameObject("Cover wall test");
            m_wall.layer = CoverLayer;
            m_wall.transform.position = Origin + new Vector3(0, 1.1f, 1.1f);
            BoxCollider wall = m_wall.AddComponent<BoxCollider>();
            wall.size = new Vector3(10, 2.2f, .3f);
            m_cover = m_wall.AddComponent<Cover>();
            Physics.SyncTransforms();
        }

        [TearDown]
        public void TearDown()
        {
            if (m_character) Object.Destroy(m_character);
            if (m_wall) Object.Destroy(m_wall);
            if (m_ceiling) Object.Destroy(m_ceiling);
        }

        [Test]
        public void BaseMovementCoverCommandsThrowNotImplemented()
        {
            IMovement movement = m_character.AddComponent<Movement>();
            Assert.IsFalse(movement.IsInCover);
            Assert.IsFalse(movement.IsExposed);
            Assert.Throws<NotImplementedException>(() => movement.TakeCover());
            Assert.Throws<NotImplementedException>(() => movement.LeaveCover());
            Assert.Throws<NotImplementedException>(() => movement.SetCoverExposure(true, Vector3.forward));
        }

        [UnityTest]
        public IEnumerator SharedWeaponFireExposesAndHidesWithoutAnAnimator()
        {
            WeaponsSystem weapons = AddWeapons();
            yield return null;
            weapons.Aim(Vector3.forward);
            weapons.BeginFire();
            m_movement.TakeCover();
            yield return new WaitForFixedUpdate();
            Assert.IsFalse(m_movement.IsExposed);
            yield return new WaitForSeconds(.55f);
            yield return new WaitForFixedUpdate();
            Assert.IsTrue(m_movement.IsExposed);
            Assert.That(m_capsule.height, Is.EqualTo(1.8f).Within(.001f));
            weapons.EndFire();
            yield return new WaitForFixedUpdate();
            Assert.IsFalse(m_movement.IsExposed);
            Assert.That(m_capsule.height, Is.EqualTo(1).Within(.001f));

            weapons.BeginFire();
            yield return new WaitForFixedUpdate();
            Assert.IsTrue(m_movement.IsExposed);
            weapons.CancelInput();
            yield return new WaitForFixedUpdate();
            Assert.IsFalse(m_movement.IsExposed);
            weapons.BeginFire();
            yield return new WaitForFixedUpdate();
            Assert.IsTrue(m_movement.IsExposed);
            weapons.Lock(this);
            yield return new WaitForFixedUpdate();
            Assert.IsFalse(m_movement.IsExposed);
            weapons.Unlock(this);
            weapons.BeginFire();
            yield return new WaitForFixedUpdate();
            Assert.IsTrue(m_movement.IsExposed);
            weapons.enabled = false;
            weapons.BeginFire();
            yield return new WaitForFixedUpdate();
            Assert.IsFalse(weapons.IsFireHeld);
            Assert.IsFalse(m_movement.IsExposed);
            weapons.enabled = true;
            yield return new WaitForFixedUpdate();
            Assert.IsFalse(weapons.IsFireHeld);
            Assert.IsFalse(m_movement.IsExposed);
        }

        [UnityTest]
        public IEnumerator ExposureWaitsForTheLeanOutDuration()
        {
            WeaponsSystem weapons = AddWeapons();
            SetField(typeof(CoverMovement), m_movement, "m_exposeDuration", .2f);
            yield return null;
            weapons.Aim(Vector3.forward);
            m_movement.TakeCover();
            yield return new WaitForSeconds(.55f);
            weapons.BeginFire();
            yield return new WaitForFixedUpdate();
            Assert.That(m_movement.ExposureProgress, Is.GreaterThan(0).And.LessThan(1));
            Assert.IsFalse(m_movement.IsExposed);
            Assert.That(m_capsule.height, Is.EqualTo(1.8f).Within(.001f));
            yield return new WaitForSeconds(.25f);
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(1, m_movement.ExposureProgress);
            Assert.IsTrue(m_movement.IsExposed);
            weapons.EndFire();
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(0, m_movement.ExposureProgress);
            Assert.IsFalse(m_movement.IsExposed);
        }

        [UnityTest]
        public IEnumerator SharedWeaponFireStillRequiresAClearHighCoverEdge()
        {
            WeaponsSystem weapons = AddWeapons();
            SetField(typeof(Cover), m_cover, "m_type", CoverType.High);
            yield return null;
            weapons.Aim(Vector3.forward);
            weapons.BeginFire();
            m_movement.TakeCover();
            yield return new WaitForSeconds(.55f);
            yield return new WaitForFixedUpdate();
            Assert.IsFalse(m_movement.IsExposed);
            m_movement.LeaveCover();
            m_body.position = Origin + Vector3.right * 4.65f;
            Physics.SyncTransforms();
            m_movement.TakeCover();
            yield return new WaitForSeconds(.55f);
            yield return new WaitForFixedUpdate();
            Assert.IsTrue(m_movement.IsExposed);
            weapons.EndFire();
            yield return new WaitForFixedUpdate();
            Assert.IsFalse(m_movement.IsExposed);
        }

        [UnityTest]
        public IEnumerator LowCoverExposureRestoresStandingColliderAndHidesOnRelease()
        {
            AddWeapons();
            yield return null;
            IMovement movement = m_entity.Movement;
            movement.TakeCover();
            movement.SetCoverExposure(true, Vector3.forward);
            yield return new WaitForFixedUpdate();
            Assert.IsFalse(movement.IsExposed);
            yield return new WaitForSeconds(.55f);
            yield return new WaitForFixedUpdate();
            Assert.IsTrue(movement.IsExposed);
            Assert.IsTrue(movement.IsInCover);
            Assert.That(m_capsule.height, Is.EqualTo(1.8f).Within(.001f));
            Assert.That(m_capsule.center.y, Is.EqualTo(.9f).Within(.001f));

            movement.Move(Vector3.right);
            yield return new WaitForFixedUpdate();
            Assert.That(m_body.linearVelocity.x, Is.EqualTo(movement.Speed * .5f).Within(.05f));
            movement.SetCoverExposure(false, Vector3.zero);
            Assert.IsFalse(movement.IsExposed);
            Assert.That(m_capsule.height, Is.EqualTo(1).Within(.001f));
            Assert.AreEqual(CoverState.Holding, m_movement.State);
        }

        [UnityTest]
        public IEnumerator LowCoverExposureRequiresClearanceAndClearsOnLock()
        {
            yield return null;
            m_movement.TakeCover();
            yield return new WaitForSeconds(.55f);
            m_ceiling = new GameObject("Exposure obstruction test");
            m_ceiling.transform.position = m_body.position + Vector3.up * 1.5f;
            m_ceiling.AddComponent<BoxCollider>().size = new Vector3(1, .2f, 1);
            Physics.SyncTransforms();

            m_movement.SetCoverExposure(true, Vector3.forward);
            yield return new WaitForFixedUpdate();
            Assert.IsFalse(m_movement.IsExposed);
            Assert.That(m_capsule.height, Is.EqualTo(1).Within(.001f));
            m_ceiling.SetActive(false);
            Physics.SyncTransforms();
            yield return new WaitForFixedUpdate();
            Assert.IsTrue(m_movement.IsExposed);

            m_movement.Lock(this);
            Assert.IsFalse(m_movement.IsExposed);
            Assert.IsFalse(m_movement.IsInCover);
            m_movement.Unlock(this);
            m_movement.TakeCover();
            yield return new WaitForSeconds(.55f);
            Assert.IsFalse(m_movement.IsExposed);
        }

        [UnityTest]
        public IEnumerator HighCoverCannotExposeFromTheMiddle()
        {
            SetField(typeof(Cover), m_cover, "m_type", CoverType.High);
            yield return null;
            m_movement.TakeCover();
            yield return new WaitForSeconds(.55f);
            Vector3 coveredPosition = m_body.position;
            m_movement.SetCoverExposure(true, Vector3.forward);
            yield return new WaitForSeconds(.4f);
            Assert.IsFalse(m_movement.IsExposed);
            Assert.That(Vector3.Distance(m_body.position, coveredPosition), Is.LessThan(.02f));
        }

        [UnityTest]
        public IEnumerator HighCoverKeepsBodyCoveredAtLeftEdge() => CheckHighCoverExposure(-1);

        [UnityTest]
        public IEnumerator HighCoverKeepsBodyCoveredAtRightEdge() => CheckHighCoverExposure(1);

        [UnityTest]
        public IEnumerator HighCoverExposureFollowsRotatedWall() => CheckHighCoverExposure(1, 45);

        private IEnumerator CheckHighCoverExposure(float side, float angle = 0)
        {
            SetField(typeof(Cover), m_cover, "m_type", CoverType.High);
            Quaternion rotation = Quaternion.Euler(0, angle, 0);
            m_wall.transform.SetPositionAndRotation(Origin + rotation * new Vector3(0, 1.1f, 1.1f), rotation);
            m_body.position = Origin + rotation * new Vector3(side * 4.65f, 0, 0);
            Physics.SyncTransforms();
            yield return null;
            IMovement movement = m_entity.Movement;
            movement.TakeCover();
            yield return new WaitForSeconds(.55f);
            Vector3 coveredPosition = m_body.position;
            movement.SetCoverExposure(true, rotation * Vector3.forward);
            yield return new WaitForSeconds(.4f);
            Assert.IsTrue(movement.IsExposed);
            Assert.IsTrue(movement.IsInCover);
            Assert.That(Vector3.Distance(m_body.position, coveredPosition), Is.LessThan(.02f));
            Assert.That(Vector3.Dot(m_character.transform.forward, m_movement.CoverNormal), Is.GreaterThan(.99f));
            Assert.That(Vector3.Dot(m_movement.ExposureDirection, rotation * Vector3.right * side), Is.GreaterThan(.99f));
            Assert.That(m_capsule.height, Is.EqualTo(1.8f).Within(.001f));

            movement.SetCoverExposure(false, Vector3.zero);
            yield return new WaitForSeconds(.4f);
            Assert.IsFalse(movement.IsExposed);
            Assert.AreEqual(Vector3.zero, m_movement.ExposureDirection);
            Assert.IsTrue(movement.IsInCover);
            Assert.That(Vector3.Distance(m_body.position, coveredPosition), Is.LessThan(.03f));
            Assert.That(Vector3.Dot(m_character.transform.forward, m_movement.CoverNormal), Is.GreaterThan(.99f));
        }

        [UnityTest]
        public IEnumerator HighCoverExposureRejectsBlockedEdgesAndRechecksClearance()
        {
            SetField(typeof(Cover), m_cover, "m_type", CoverType.High);
            m_body.position = Origin + Vector3.right * 4.65f;
            Physics.SyncTransforms();
            yield return null;
            m_movement.TakeCover();
            yield return new WaitForSeconds(.55f);
            m_ceiling = new GameObject("Blocked edge test");
            m_ceiling.transform.position = m_body.position + new Vector3(.6f, .9f, 0);
            m_ceiling.AddComponent<BoxCollider>().size = new Vector3(.2f, 1.8f, .2f);
            Physics.SyncTransforms();

            m_movement.SetCoverExposure(true, Vector3.forward);
            yield return new WaitForFixedUpdate();
            Assert.IsFalse(m_movement.IsExposed);
            m_ceiling.SetActive(false);
            Physics.SyncTransforms();
            yield return new WaitForSeconds(.4f);
            Assert.IsTrue(m_movement.IsExposed);

            m_wall.GetComponent<BoxCollider>().size = new Vector3(20, 2.2f, .3f);
            Physics.SyncTransforms();
            yield return new WaitForFixedUpdate();
            Assert.IsFalse(m_movement.IsExposed);
            m_movement.enabled = false;
            Assert.IsFalse(m_movement.IsInCover);
            Assert.IsFalse(m_movement.IsExposed);
        }

        [UnityTest]
        public IEnumerator LowCoverWorksWithoutAnimatorOrInput() => CheckCover(CoverType.Low);

        [UnityTest]
        public IEnumerator HighCoverWorksWithoutAnimatorOrInput() => CheckCover(CoverType.High);

        private IEnumerator CheckCover(CoverType type)
        {
            SetField(typeof(Cover), m_cover, "m_type", type);
            yield return null;
            Assert.AreSame(m_cover, m_movement.AvailableCover);
            Assert.IsTrue(m_cover.IsAvailable);

            IMovement movement = m_entity.Movement;
            Assert.IsNotNull(movement);
            movement.TakeCover();
            Assert.IsTrue(movement.IsInCover);
            Assert.AreEqual(CoverState.Entering, m_movement.State);
            Assert.IsFalse(m_cover.IsAvailable);
            Assert.That(m_capsule.height, Is.EqualTo(type == CoverType.Low ? 1 : 1.8f).Within(.001f));
            Assert.That(m_movement.CoverSpeed, Is.EqualTo(movement.Speed * .3f).Within(.001f));

            movement.Move(Vector3.right);
            yield return new WaitForFixedUpdate();
            Assert.That(m_body.linearVelocity.magnitude, Is.LessThanOrEqualTo(movement.Speed * .3f + .01f));
            yield return new WaitForSeconds(.55f);
            Assert.AreEqual(CoverState.Holding, m_movement.State);
            Assert.That(m_movement.CoverSpeed, Is.EqualTo(movement.Speed * .5f).Within(.001f));
            yield return new WaitForFixedUpdate();
            Assert.That(m_body.linearVelocity.x, Is.EqualTo(movement.Speed * .5f).Within(.05f));

            movement.LeaveCover();
            Assert.IsFalse(movement.IsInCover);
            Assert.AreEqual(CoverState.Leaving, m_movement.State);
            Assert.That(m_capsule.height, Is.EqualTo(1.8f).Within(.001f));
            yield return null;
            Assert.That(m_body.linearVelocity.x, Is.EqualTo(movement.Speed * .3f).Within(.05f));
            yield return new WaitForSeconds(.35f);
            Assert.AreEqual(CoverState.None, m_movement.State);
            yield return null;
            Assert.That(m_body.linearVelocity.x, Is.EqualTo(movement.Speed).Within(.05f));
        }

        [UnityTest]
        public IEnumerator CoverRespectsLocksDisabledComponentsAndInterruptedTransitions()
        {
            yield return null;
            m_movement.Lock(this);
            m_movement.TakeCover();
            Assert.IsFalse(m_movement.IsInCover);
            Assert.IsFalse(m_cover.IsAvailable);
            m_movement.Unlock(this);

            m_movement.TakeCover();
            m_movement.LeaveCover();
            Assert.AreEqual(CoverState.Leaving, m_movement.State);
            m_movement.TakeCover();
            Assert.AreEqual(CoverState.Entering, m_movement.State);
            m_movement.Lock(this);
            Assert.AreEqual(CoverState.None, m_movement.State);
            Assert.That(m_capsule.height, Is.EqualTo(1.8f).Within(.001f));
            m_movement.Unlock(this);

            m_movement.TakeCover();
            m_cover.enabled = false;
            yield return null;
            Assert.IsFalse(m_movement.IsInCover);
            m_movement.TakeCover();
            Assert.IsFalse(m_movement.IsInCover);
            m_cover.enabled = true;

            m_movement.TakeCover();
            m_movement.enabled = false;
            Assert.IsFalse(m_movement.IsInCover);
            m_movement.TakeCover();
            Assert.IsFalse(m_movement.IsInCover);
            Assert.That(m_capsule.height, Is.EqualTo(1.8f).Within(.001f));
        }

        [UnityTest]
        public IEnumerator LowCoverCannotBeLeftWithoutStandingClearance()
        {
            yield return null;
            m_movement.TakeCover();
            yield return new WaitForSeconds(.55f);
            m_ceiling = new GameObject("Standing obstruction test");
            m_ceiling.transform.position = m_body.position + Vector3.up * 1.5f;
            m_ceiling.AddComponent<BoxCollider>().size = new Vector3(1, .2f, 1);
            Physics.SyncTransforms();

            m_movement.LeaveCover();
            Assert.IsTrue(m_movement.IsInCover);
            Assert.That(m_capsule.height, Is.EqualTo(1).Within(.001f));
            m_ceiling.SetActive(false);
            Physics.SyncTransforms();
            m_movement.LeaveCover();
            Assert.IsFalse(m_movement.IsInCover);
        }

        [UnityTest]
        public IEnumerator InputControllerTogglesCoverThroughTheInterface()
        {
            PlayerInputController input = m_character.AddComponent<PlayerInputController>();
            SetField(typeof(PlayerInputController), input, "player", m_entity);
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            using InputAction action = new(type: InputActionType.Button, binding: CoverBinding);
            action.performed += input.OnCover;
            action.Enable();
            try
            {
                yield return null;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space));
                InputSystem.Update();
                Assert.IsTrue(m_movement.IsInCover);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                InputSystem.Update();
                Assert.IsTrue(m_movement.IsInCover);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space));
                InputSystem.Update();
                Assert.IsFalse(m_movement.IsInCover);
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard);
            }
        }

        private WeaponsSystem AddWeapons()
        {
            WeaponsSystem weapons = m_character.AddComponent<WeaponsSystem>();
            SetField(typeof(EntitySystem), weapons, "m_entity", m_entity);
            SetField(typeof(Entity), m_entity, "m_weaponsSystem", weapons);
            return weapons;
        }

        private static void SetField(Type type, object target, string name, object value) =>
            type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }
}
