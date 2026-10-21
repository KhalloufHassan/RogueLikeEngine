using UnityEngine;

namespace RogueLikeEngine.Audio
{
    /// <summary>
    /// Playback of a <see cref="ChargeSound"/>: loops its clip and applies the charge curves every frame.
    /// </summary>
    public class ChargeSoundPlayback : ISoundPlayback
    {
        private readonly ChargeSound m_sound;
        private float m_pitch;

        internal ChargeSoundPlayback(ChargeSound sound) => m_sound = sound;

        public SoundHandle Handle { get; set; }

        public void Start(SoundEmitter emitter)
        {
            m_pitch = m_sound.Clips.RandomPitch;
            AudioSource source = emitter.Source;
            source.clip = m_sound.Clips.RandomClip;
            source.priority = m_sound.Priority;
            source.spatialBlend = m_sound.SpatialBlend;
            source.loop = true;
            Apply(emitter);
            source.Play();
        }

        public bool Update(SoundEmitter emitter, float deltaTime)
        {
            Apply(emitter);
            return true;
        }

        public void Release(SoundEmitter emitter) => Handle.Stop();

        public void Recycle()
        {
            Handle = default;
            m_sound.Recycle(this);
        }

        private void Apply(SoundEmitter emitter)
        {
            emitter.Source.pitch = m_pitch * m_sound.PitchOverCharge.Evaluate(emitter.Intensity) * emitter.PitchMultiplier * emitter.TimeScale;
            emitter.Source.volume = m_sound.Clips.Volume * m_sound.VolumeOverCharge.Evaluate(emitter.Intensity) * emitter.VolumeMultiplier;
        }
    }
}
