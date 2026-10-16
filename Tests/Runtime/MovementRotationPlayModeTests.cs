using System.Collections;
using System.Reflection;
using NUnit.Framework;
using RogueLikeEngine.Systems.Entities;
using RogueLikeEngine.Systems.Movements;
using RogueLikeEngine.Systems.Weapons;
using UnityEngine;
using UnityEngine.TestTools;

namespace RogueLikeEngine.Tests.PlayMode
{
    public class MovementRotationPlayModeTests
    {
        private GameObject m_object;

        [TearDown]
        public void TearDown()
        {
            if (m_object) Object.Destroy(m_object);
        }

        [UnityTest]
        public IEnumerator Body3D_TurnsInPhysics_WithoutOverwritingInterpolation() => CheckRotation(false);

        [UnityTest]
        public IEnumerator Body2D_TurnsInPhysics_WithoutOverwritingInterpolation() => CheckRotation(true);

        private IEnumerator CheckRotation(bool use2D)
        {
            m_object = new GameObject("Movement rotation test");
            m_object.SetActive(false);
            Rigidbody body3D = null;
            Rigidbody2D body2D = null;
            if (use2D)
            {
                body2D = m_object.AddComponent<Rigidbody2D>();
                body2D.gravityScale = 0;
                body2D.interpolation = RigidbodyInterpolation2D.Interpolate;
            }
            else
            {
                body3D = m_object.AddComponent<Rigidbody>();
                body3D.useGravity = false;
                body3D.interpolation = RigidbodyInterpolation.Interpolate;
                body3D.constraints = RigidbodyConstraints.FreezeRotation;
            }

            Entity entity = m_object.AddComponent<Entity>();
            Movement movement = m_object.AddComponent<Movement>();
            WeaponsSystem weapons = m_object.AddComponent<WeaponsSystem>();
            typeof(Entity).GetField("m_movement", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(entity, movement);
            typeof(Entity).GetField("m_weaponsSystem", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(entity, weapons);
            FieldInfo owner = typeof(EntitySystem).GetField("m_entity", BindingFlags.Instance | BindingFlags.NonPublic);
            owner.SetValue(movement, entity);
            owner.SetValue(weapons, entity);
            typeof(Movement).GetField("m_rotateToAimDirection", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(movement, true);
            typeof(Movement).GetField("m_turnSpeed", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(movement, 90f);
            m_object.SetActive(true);
            yield return null;

            movement.Move(Vector3.right);
            weapons.Aim(Vector3.left);
            Quaternion renderedRotation = m_object.transform.rotation;
            typeof(Movement).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(movement, null);
            Assert.That(Quaternion.Angle(renderedRotation, m_object.transform.rotation), Is.LessThan(1e-3f),
                "Render updates must not overwrite the interpolated transform.");

            yield return new WaitForFixedUpdate();

            float angle = use2D ? body2D.rotation : Vector3.SignedAngle(Vector3.forward, body3D.rotation * Vector3.forward, Vector3.up);
            float step = 90f * Time.fixedDeltaTime;
            Assert.That(angle, Is.EqualTo(use2D ? step : -step).Within(0.1f),
                "Aim must determine facing independently of movement, at the configured turn speed.");
            Assert.Greater(movement.Position.x, 0f, "The character must still walk right while aiming left.");

            weapons.StopAim();
            yield return new WaitForFixedUpdate();

            angle = use2D ? body2D.rotation : Vector3.SignedAngle(Vector3.forward, body3D.rotation * Vector3.forward, Vector3.up);
            Assert.That(Mathf.DeltaAngle(0, angle), Is.EqualTo(0f).Within(0.1f),
                "Without aim, facing must turn back toward movement.");

            weapons.Aim(Vector3.left);
            movement.Lock(this);
            if (use2D) body2D.angularVelocity = 0;
            Quaternion lockedRotation = use2D ? Quaternion.Euler(0, 0, body2D.rotation) : body3D.rotation;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            Quaternion currentRotation = use2D ? Quaternion.Euler(0, 0, body2D.rotation) : body3D.rotation;
            Assert.That(Quaternion.Angle(lockedRotation, currentRotation), Is.LessThan(1e-3f),
                "Locked movement must stop driving rotation.");
        }
    }
}
