namespace RogueLikeEngine.Audio
{
    /// <summary>
    /// One playback started by <see cref="SoundManager"/>. It only controls that playback: once it ends or is stopped,
    /// the handle goes inactive and never affects a later sound reusing the same pooled emitter.
    /// The default handle (returned for skipped sounds) is inactive.
    /// </summary>
    public readonly struct SoundHandle
    {
        private readonly SoundEmitter m_emitter;
        private readonly int m_playId;

        public SoundHandle(SoundEmitter emitter, int playId)
        {
            m_emitter = emitter;
            m_playId = playId;
        }

        /// <summary>True while the sound is playing or waiting for its delay.</summary>
        public bool IsActive => m_emitter && m_emitter.IsCurrent(m_playId);

        /// <summary>Multiplies the sound's own pitch, on top of the time scale.</summary>
        public void SetPitch(float multiplier)
        {
            if (m_emitter) m_emitter.SetPitch(m_playId, multiplier);
        }

        /// <summary>Multiplies the sound's own volume.</summary>
        public void SetVolume(float multiplier)
        {
            if (m_emitter) m_emitter.SetVolume(m_playId, multiplier);
        }

        /// <summary>
        /// Drives sounds that react to a 0 to 1 value, such as a <see cref="ChargeSound"/>'s charge. Other sounds ignore it.
        /// </summary>
        public void SetIntensity(float intensity)
        {
            if (m_emitter) m_emitter.SetIntensity(m_playId, intensity);
        }

        /// <summary>
        /// Ends the sound gracefully: a sequence fades out and plays its end part, a looping or charge sound stops, a
        /// one-shot keeps playing to its end.
        /// </summary>
        public void Release()
        {
            if (m_emitter) m_emitter.Release(m_playId);
        }

        /// <summary>Fades the sound out over a few milliseconds, then frees its emitter.</summary>
        public void Stop()
        {
            if (m_emitter) m_emitter.Stop(m_playId);
        }
    }
}
