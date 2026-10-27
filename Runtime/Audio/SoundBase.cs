using System;
using System.Collections.Generic;
using UnityEngine;

namespace RogueLikeEngine.Audio
{
    /// <summary>
    /// Shared settings and limits of the library sounds. Reuses its playbacks, so playing doesn't allocate once warmed up.
    /// </summary>
    [Serializable]
    public abstract class SoundBase<TPlayback> : ISound where TPlayback : class, ISoundPlayback
    {
        [SerializeField] private AudioMixerType m_channel;
        [SerializeField, Range(0, 256)] private int m_priority = 128;
        [SerializeField, Range(0f, 1f)] private float m_spatialBlend = 1;
        [Tooltip("Plays at normal speed during slow motion and pause (UI, music).")]
        [SerializeField] private bool m_ignoreTimeScale;
        [Tooltip("Seconds to wait before playing.")]
        [SerializeField, Min(0)] private float m_delay;
        [Tooltip("Stops the sound after this many seconds. Zero lets it end on its own or when stopped through its handle.")]
        [SerializeField, Min(0)] private float m_duration;
        [Tooltip("Minimum real seconds between two plays of this sound, requests in between are skipped.")]
        [SerializeField, Min(0)] private float m_minInterval = 0.1f;
        [Tooltip("Maximum instances playing at once, starting another stops the oldest. Zero is unlimited.")]
        [SerializeField, Min(0)] private int m_maxInstances;

        [NonSerialized] private double m_lastPlayTime = double.NegativeInfinity;
        [NonSerialized] private List<TPlayback> m_active;
        [NonSerialized] private Stack<TPlayback> m_free;

        public AudioMixerType Channel => m_channel;
        public bool IgnoreTimeScale => m_ignoreTimeScale;
        public float Delay => m_delay;
        public float Duration => m_duration;
        public int Priority => m_priority;
        public float SpatialBlend => m_spatialBlend;

        protected abstract bool HasClips { get; }

        public ISoundPlayback CreatePlayback()
        {
            if (!HasClips)
            {
                Debug.LogWarning("Playing a sound with no clips");
                return null;
            }

            double now = Time.realtimeSinceStartupAsDouble;
            if (now >= m_lastPlayTime && now - m_lastPlayTime < m_minInterval) return null;
            m_lastPlayTime = now;
            m_active ??= new List<TPlayback>();
            if (m_maxInstances > 0 && m_active.Count >= m_maxInstances)
            {
                TPlayback oldest = m_active[0];
                m_active.RemoveAt(0);
                oldest.Handle.Stop();
            }

            TPlayback playback = m_free is { Count: > 0 } ? m_free.Pop() : CreateNewPlayback();
            m_active.Add(playback);
            return playback;
        }

        /// <summary>Called by playbacks from <see cref="ISoundPlayback.Recycle"/>.</summary>
        public void Recycle(TPlayback playback)
        {
            m_active?.Remove(playback);
            (m_free ??= new Stack<TPlayback>()).Push(playback);
        }

        protected abstract TPlayback CreateNewPlayback();
    }
}
