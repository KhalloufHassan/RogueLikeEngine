using UnityEngine;

namespace RogueLikeEngine.Audio
{
    /// <summary>
    /// Playback of a <see cref="Sound"/>. Tracks progress through the clip in clip seconds, so pitch and time scale
    /// changes while playing never cut the clip short or hold the emitter after it ends.
    /// </summary>
    public class SoundPlayback : ISoundPlayback
    {
        private readonly Sound m_sound;
        private AudioClip m_clip;
        private float m_pitch;
        private float m_progress;

        internal SoundPlayback(Sound sound) => m_sound = sound;

        public SoundHandle Handle { get; set; }

        public void Start(SoundEmitter emitter)
        {
            m_clip = m_sound.Clips.RandomClip;
            m_pitch = m_sound.Clips.RandomPitch;
            m_progress = 0;
            AudioSource source = emitter.Source;
            source.clip = m_clip;
            source.priority = m_sound.Priority;
            source.spatialBlend = m_sound.SpatialBlend;
            source.loop = m_sound.Loop;
            Apply(emitter);
            source.Play();
        }

        public bool Update(SoundEmitter emitter, float deltaTime)
        {
            m_progress += deltaTime * m_pitch * emitter.PitchMultiplier;
            Apply(emitter);
            return m_sound.Loop || m_progress < m_clip.length;
        }

        /// <summary>Stops a looping sound; a one-shot keeps playing to its end.</summary>
        public void Release(SoundEmitter emitter)
        {
            if (m_sound.Loop) Handle.Stop();
        }

        public void Recycle()
        {
            m_clip = null;
            Handle = default;
            m_sound.Recycle(this);
        }

        private void Apply(SoundEmitter emitter)
        {
            float fade = m_sound.FadeCurve?.Evaluate(m_progress / m_clip.length) ?? 1;
            emitter.Source.pitch = m_pitch * emitter.PitchMultiplier * emitter.TimeScale;
            emitter.Source.volume = m_sound.Clips.Volume * fade * emitter.VolumeMultiplier;
        }
    }
}
