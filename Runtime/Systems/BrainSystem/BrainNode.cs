using System;

namespace RogueLikeEngine.Systems.Brains
{
    /// <summary>A per-brain action. Stop is called on completion and interruption.</summary>
    [Serializable]
    public abstract class BrainNode
    {
        [NonSerialized] private BrainStatus? m_status;
        [NonSerialized] private bool m_running;
        [NonSerialized] private int m_version;

        public BrainStatus Tick(BrainContext context)
        {
            if (m_status.HasValue && m_status != BrainStatus.Running) return m_status.Value;
            int version = m_version;
            if (!m_running)
            {
                m_running = true;
                OnStart(context);
            }
            if (version != m_version) return BrainStatus.Failure;
            BrainStatus status = OnTick(context);
            if (version != m_version) return BrainStatus.Failure;
            m_status = status;
            if (m_status != BrainStatus.Running) Stop(context);
            return status;
        }

        public virtual void Reset(BrainContext context)
        {
            m_version++;
            Stop(context);
            m_status = null;
        }

        private void Stop(BrainContext context)
        {
            if (!m_running) return;
            m_running = false;
            OnStop(context);
        }

        protected virtual void OnStart(BrainContext context) { }
        protected abstract BrainStatus OnTick(BrainContext context);
        protected virtual void OnStop(BrainContext context) { }
    }
}
