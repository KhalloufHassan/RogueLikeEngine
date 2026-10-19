using System.Collections;
using System.Reflection;
using NUnit.Framework;
using RogueLikeEngine.Systems.Abilities;
using UnityEngine;
using UnityEngine.TestTools;

namespace RogueLikeEngine.Tests.PlayMode.Input
{
    public class AbilityCooldownTests
    {
        private const string CooldownDurationField = "m_cooldownDuration";

        private GameObject m_object;
        private RecordingAbility m_ability;
        private float m_timeScale;

        [SetUp]
        public void SetUp()
        {
            m_timeScale = Time.timeScale;
            Time.timeScale = 1;
            m_object = new GameObject("Ability");
            m_ability = m_object.AddComponent<RecordingAbility>();
            SetDuration(5);
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = m_timeScale;
            Object.Destroy(m_object);
        }

        [Test]
        public void NewAbilityIsReadyAndCancellingDoesNotStartCooldown()
        {
            Assert.AreEqual(0, m_ability.CooldownRemaining);
            m_ability.BeginUse();
            m_ability.CancelUse();
            Assert.AreEqual(0, m_ability.CooldownRemaining);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void NonPositiveDurationHasNoCooldown(float duration)
        {
            SetDuration(duration);
            m_ability.StartTestCooldown();
            Assert.AreEqual(0, m_ability.CooldownRemaining);
        }

        [UnityTest]
        public IEnumerator CommittedCooldownExpiresAndDoesNotAffectOtherInstances()
        {
            RecordingAbility other = m_object.AddComponent<RecordingAbility>();
            SetDuration(.05f);
            m_ability.StartTestCooldown();
            Assert.That(m_ability.CooldownRemaining, Is.EqualTo(.05f).Within(.001f));
            Assert.AreEqual(0, other.CooldownRemaining);
            yield return new WaitForSeconds(.08f);
            Assert.AreEqual(0, m_ability.CooldownRemaining);
        }

        [UnityTest]
        public IEnumerator CooldownUsesGameTimeAndSurvivesDisabling()
        {
            Time.timeScale = 0;
            m_ability.StartTestCooldown();
            m_ability.enabled = false;
            yield return new WaitForSecondsRealtime(.05f);
            Assert.That(m_ability.CooldownRemaining, Is.EqualTo(5).Within(.001f));
            m_ability.enabled = true;
            Assert.That(m_ability.CooldownRemaining, Is.EqualTo(5).Within(.001f));

            Time.timeScale = .25f;
            float startedAt = Time.timeSinceLevelLoad;
            yield return new WaitForSecondsRealtime(.1f);
            float elapsed = Time.timeSinceLevelLoad - startedAt;
            Assert.Greater(elapsed, 0);
            Assert.That(m_ability.CooldownRemaining, Is.EqualTo(5 - elapsed).Within(.001f));
        }

        private void SetDuration(float duration) => typeof(Ability).GetField(CooldownDurationField, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(m_ability, duration);
    }
}
