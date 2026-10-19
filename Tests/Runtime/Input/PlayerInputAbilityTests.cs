using System.Collections;
using System.Reflection;
using NUnit.Framework;
using RogueLikeEngine.Input;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

namespace RogueLikeEngine.Tests.PlayMode.Input
{
    public class PlayerInputAbilityTests
    {
        private const string AbilityBinding = "<Keyboard>/leftCtrl";

        private GameObject m_player;
        private PlayerInputController m_input;
        private RecordingAbility m_ability;
        private Keyboard m_keyboard;
        private InputAction m_action;

        [SetUp]
        public void SetUp()
        {
            m_player = new GameObject("Player");
            m_ability = m_player.AddComponent<RecordingAbility>();
            m_input = m_player.AddComponent<PlayerInputController>();
            typeof(PlayerInputController).GetField("ability", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(m_input, m_ability);
            m_keyboard = InputSystem.AddDevice<Keyboard>();
            m_action = new InputAction(type: InputActionType.Button, binding: AbilityBinding);
            m_action.started += m_input.OnAbility;
            m_action.performed += m_input.OnAbility;
            m_action.canceled += m_input.OnAbility;
            m_action.Enable();
        }

        [TearDown]
        public void TearDown()
        {
            m_action.Dispose();
            InputSystem.RemoveDevice(m_keyboard);
            Object.Destroy(m_player);
        }

        [UnityTest]
        public IEnumerator PressAndReleaseBeginAndEndTheAbility()
        {
            yield return null;
            Press(true);
            Assert.AreEqual(1, m_ability.Begins);
            Assert.AreEqual(0, m_ability.Ends);
            Press(false);
            Assert.AreEqual(1, m_ability.Begins);
            Assert.AreEqual(1, m_ability.Ends);
        }

        [UnityTest]
        public IEnumerator DisablingInputCancelsInsteadOfEnding()
        {
            yield return null;
            Press(true);
            m_input.enabled = false;
            Assert.AreEqual(0, m_ability.Ends);
            Assert.GreaterOrEqual(m_ability.Cancels, 1);
            Press(false);
            Assert.AreEqual(0, m_ability.Ends);
        }

        private void Press(bool pressed)
        {
            InputSystem.QueueStateEvent(m_keyboard, pressed ? new KeyboardState(Key.LeftCtrl) : new KeyboardState());
            InputSystem.Update();
        }
    }
}
