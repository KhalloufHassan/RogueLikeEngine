using RogueLikeEngine.Utils.Timers;
using UnityEngine;

namespace RogueLikeEngine.Systems.Abilities
{
    /// <summary>An ability used by holding and releasing an input, each implementation decides what both mean.</summary>
    public abstract class Ability : MonoBehaviour
    {
        [Tooltip("Cooldown in game seconds, starting when the ability commits its use")]
        [SerializeField, Min(0)] private float m_cooldownDuration;

        private AutoTimer m_cooldownTimer;

        public float CooldownRemaining => Mathf.Max(0, m_cooldownTimer.TimeLeft);

        /// <summary>Called once when the ability input is pressed. Ignore while CooldownRemaining is positive.</summary>
        public abstract void BeginUse();

        /// <summary>Called when the player releases the ability input.</summary>
        public abstract void EndUse();

        /// <summary>Aborts held input without treating interruption as a release, e.g. on focus loss.</summary>
        public abstract void CancelUse();

        /// <summary>Starts the configured cooldown when the implementation commits its use.</summary>
        protected void StartCooldown()
        {
            m_cooldownTimer = Mathf.Max(0, m_cooldownDuration);
            m_cooldownTimer.Reset();
        }
    }
}
