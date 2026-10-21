using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace RogueLikeEngine.Audio
{
    /// <summary>
    /// A set of interchangeable clips with a volume and a random pitch range.
    /// </summary>
    [Serializable]
    public class SoundClips
    {
        [SerializeField] private AudioClip[] m_clips;
        [SerializeField, Range(0f, 1f)] private float m_volume = 1;
        [SerializeField, Range(0.1f, 3f)] private float m_minPitch = 1;
        [SerializeField, Range(0.1f, 3f)] private float m_maxPitch = 1;

        public bool HasClips => m_clips is { Length: > 0 };
        public float Volume => m_volume;
        public AudioClip RandomClip => m_clips[Random.Range(0, m_clips.Length)];
        public float RandomPitch => Random.Range(m_minPitch, m_maxPitch);
    }
}
