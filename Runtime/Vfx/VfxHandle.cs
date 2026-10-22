namespace RogueLikeEngine.Vfx
{
    /// <summary>
    /// One play of a <see cref="PooledVfx"/>. It only controls that play: once it ends or is stopped, the handle goes
    /// inactive and never affects a later play reusing the same pooled instance. The default handle is inactive.
    /// </summary>
    public readonly struct VfxHandle
    {
        private readonly PooledVfx m_effect;
        private readonly int m_playId;

        public VfxHandle(PooledVfx effect, int playId)
        {
            m_effect = effect;
            m_playId = playId;
        }

        /// <summary>True while the effect plays and hasn't been stopped.</summary>
        public bool IsActive => m_effect && m_effect.IsCurrent(m_playId);

        /// <summary>Stops emitting; particles already alive finish before the effect returns to its pool.</summary>
        public void Stop()
        {
            if (m_effect) m_effect.Stop(m_playId);
        }
    }
}
