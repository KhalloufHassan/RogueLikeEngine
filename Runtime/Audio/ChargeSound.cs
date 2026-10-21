using System;
using UnityEngine;

namespace RogueLikeEngine.Audio
{
    /// <summary>
    /// A looping sound rising with a charge, for hold-to-charge actions. The caller drives the charge (0 to 1) through
    /// <see cref="SoundHandle.SetIntensity"/>, and the curves map it to pitch and volume multipliers.
    /// Plays until released or stopped.
    /// </summary>
    [Serializable]
    public class ChargeSound : SoundBase<ChargeSoundPlayback>
    {
        [SerializeField] private SoundClips m_clips = new();
        [Tooltip("Pitch multiplier over the charge, from 0 to 1.")]
        [SerializeField] private AnimationCurve m_pitchOverCharge = AnimationCurve.Linear(0, 1, 1, 2);
        [Tooltip("Volume multiplier over the charge, from 0 to 1.")]
        [SerializeField] private AnimationCurve m_volumeOverCharge = AnimationCurve.Constant(0, 1, 1);

        internal SoundClips Clips => m_clips;
        internal AnimationCurve PitchOverCharge => m_pitchOverCharge;
        internal AnimationCurve VolumeOverCharge => m_volumeOverCharge;

        protected override bool HasClips => m_clips.HasClips;

        protected override ChargeSoundPlayback CreateNewPlayback() => new(this);
    }
}
