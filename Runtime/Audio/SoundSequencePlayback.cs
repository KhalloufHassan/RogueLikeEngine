using UnityEngine;

namespace RogueLikeEngine.Audio
{
    /// <summary>
    /// Playback of a <see cref="SoundSequence"/> on two of the emitter's sources: while one part plays, the next is
    /// scheduled on the other source on the audio clock, and rescheduled every frame to follow pitch and time scale
    /// (except in the last moments before it starts, to avoid moving a source that is already playing).
    /// </summary>
    public class SoundSequencePlayback : ISoundPlayback
    {
        private const float ReleaseFadeDuration = 0.02f;
        private const double RescheduleMargin = 0.1;
        private const double PausedDelay = 3600;

        private readonly SoundSequence m_sequence;
        private int m_sourceIndex;
        private SoundClips m_part;
        private AudioClip m_clip;
        private float m_pitch;
        private float m_progress;
        private SoundClips m_nextPart;
        private AudioClip m_nextClip;
        private float m_nextPitch;
        private AudioSource m_fadingSource;
        private float m_fadingVolume;
        private float m_fade;
        private bool m_released;

        internal SoundSequencePlayback(SoundSequence sequence) => m_sequence = sequence;

        public SoundHandle Handle { get; set; }

        public void Start(SoundEmitter emitter)
        {
            m_sourceIndex = 0;
            m_released = false;
            for (int i = 0; i < 2; i++)
            {
                AudioSource source = emitter.GetSource(i);
                source.priority = m_sequence.Priority;
                source.spatialBlend = m_sequence.SpatialBlend;
            }

            Play(emitter, ValidOrNull(m_sequence.StartPart) ?? ValidOrNull(m_sequence.LoopPart) ?? m_sequence.EndPart);
        }

        public bool Update(SoundEmitter emitter, float deltaTime)
        {
            UpdateFade();
            if (m_part == null) return m_fadingSource;
            m_progress += deltaTime * m_pitch * emitter.PitchMultiplier;
            if (m_part != m_sequence.LoopPart && m_progress >= m_clip.length)
            {
                if (m_nextPart == null)
                {
                    m_part = null;
                    return m_fadingSource;
                }

                m_progress = (m_progress - m_clip.length) / m_pitch * m_nextPitch;
                m_part = m_nextPart;
                m_clip = m_nextClip;
                m_pitch = m_nextPitch;
                m_nextPart = null;
                m_sourceIndex = 1 - m_sourceIndex;
            }

            Apply(emitter);
            return true;
        }

        public void Release(SoundEmitter emitter)
        {
            if (m_released || m_part == null || m_part == m_sequence.EndPart) return;
            m_released = true;
            if (m_nextPart != null) emitter.GetSource(1 - m_sourceIndex).Stop();
            m_nextPart = null;
            FadeOut(emitter.GetSource(m_sourceIndex));
            m_sourceIndex = 1 - m_sourceIndex;
            if (m_sequence.EndPart.HasClips) Play(emitter, m_sequence.EndPart);
            else m_part = null;
        }

        public void Recycle()
        {
            m_part = m_nextPart = null;
            m_clip = m_nextClip = null;
            m_fadingSource = null;
            Handle = default;
            m_sequence.Recycle(this);
        }

        private static SoundClips ValidOrNull(SoundClips part) => part.HasClips ? part : null;

        private void Play(SoundEmitter emitter, SoundClips part)
        {
            m_part = part;
            m_clip = part.RandomClip;
            m_pitch = part.RandomPitch;
            m_progress = 0;
            m_nextPart = null;
            AudioSource source = emitter.GetSource(m_sourceIndex);
            source.clip = m_clip;
            source.loop = part == m_sequence.LoopPart;
            Apply(emitter);
            source.Play();
            if (part != m_sequence.StartPart) return;

            m_nextPart = ValidOrNull(m_sequence.LoopPart) ?? ValidOrNull(m_sequence.EndPart);
            if (m_nextPart == null) return;
            m_nextClip = m_nextPart.RandomClip;
            m_nextPitch = m_nextPart.RandomPitch;
            AudioSource next = emitter.GetSource(1 - m_sourceIndex);
            next.clip = m_nextClip;
            next.loop = m_nextPart == m_sequence.LoopPart;
            next.PlayScheduled(AudioSettings.dspTime + RemainingRealTime(emitter));
            Apply(emitter);
        }

        private void Apply(SoundEmitter emitter)
        {
            AudioSource source = emitter.GetSource(m_sourceIndex);
            source.pitch = m_pitch * emitter.PitchMultiplier * emitter.TimeScale;
            source.volume = m_part.Volume * emitter.VolumeMultiplier;
            if (m_nextPart == null) return;

            AudioSource next = emitter.GetSource(1 - m_sourceIndex);
            next.pitch = m_nextPitch * emitter.PitchMultiplier * emitter.TimeScale;
            next.volume = m_nextPart.Volume * emitter.VolumeMultiplier;
            double remaining = RemainingRealTime(emitter);
            if (remaining > RescheduleMargin)
                next.SetScheduledStartTime(AudioSettings.dspTime + remaining);
        }

        private double RemainingRealTime(SoundEmitter emitter)
        {
            float rate = m_pitch * emitter.PitchMultiplier * emitter.TimeScale;
            return rate > 0 ? (m_clip.length - m_progress) / rate : PausedDelay;
        }

        private void FadeOut(AudioSource source)
        {
            if (m_fadingSource) m_fadingSource.Stop();
            m_fadingSource = source;
            m_fadingVolume = source.volume;
            m_fade = ReleaseFadeDuration;
        }

        private void UpdateFade()
        {
            if (!m_fadingSource) return;
            m_fade -= Time.unscaledDeltaTime;
            if (m_fade > 0)
            {
                m_fadingSource.volume = m_fadingVolume * m_fade / ReleaseFadeDuration;
                return;
            }

            m_fadingSource.Stop();
            m_fadingSource = null;
        }
    }
}
