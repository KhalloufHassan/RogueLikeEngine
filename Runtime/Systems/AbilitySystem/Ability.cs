using UnityEngine;

namespace RogueLikeEngine.Systems.Abilities
{
    /// <summary>An ability used by holding and releasing an input, each implementation decides what both mean.</summary>
    public abstract class Ability : MonoBehaviour
    {
        /// <summary>Called once when the ability input is pressed.</summary>
        public abstract void BeginUse();

        /// <summary>Called when the player releases the ability input.</summary>
        public abstract void EndUse();

        /// <summary>Aborts held input without treating interruption as a release, e.g. on focus loss.</summary>
        public abstract void CancelUse();
    }
}
