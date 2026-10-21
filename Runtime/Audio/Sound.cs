using System;
using UnityEngine;

namespace RogueLikeEngine.Audio
{
    /// <summary>
    /// Plays one random clip, once or looping.
    /// </summary>
    [Serializable]
    public class Sound : SoundBase<SoundPlayback>
    {
        [SerializeField] private SoundClips m_clips = new();
        [Tooltip("Plays until stopped or released through its handle, or for its duration.")]
        [SerializeField] private bool m_loop;
        [Tooltip("Volume over the clip's normalized progress.")]
        [SerializeField] private bool m_useFadeCurve;
        [SerializeField] private AnimationCurve m_fadeCurve = AnimationCurve.Constant(0, 1, 1);

        internal SoundClips Clips => m_clips;
        internal bool Loop => m_loop;
        internal AnimationCurve FadeCurve => m_useFadeCurve ? m_fadeCurve : null;

        protected override bool HasClips => m_clips.HasClips;

        protected override SoundPlayback CreateNewPlayback() => new(this);
    }
}
