using RogueLikeEngine.Optimization.Pooling;
using UnityEngine;
using UnityEngine.VFX;

namespace RogueLikeEngine.Vfx
{
    /// <summary>
    /// Visual effect prefab played through <see cref="VfxManager"/>. Plays every ParticleSystem and VFX Graph under it,
    /// follows an optional target, and returns to its pool once all of them have finished. Looping effects play until
    /// stopped through their <see cref="VfxHandle"/>. Each play gets a new id, so stale handles can't affect the next one.
    /// </summary>
    public class PooledVfx : MonoBehaviour, IPoolObject
    {
        private const int StartupFrames = 2;

        private ParticleSystem[] m_particleSystems;
        private VisualEffect[] m_visualEffects;
        private Transform m_target;
        private int m_playId;
        private int m_startFrame;
        private bool m_stopping;

        public IPool ParentPool { get; set; }
        public bool IsDisposed { get; set; }

        private bool IsAlive
        {
            get
            {
                foreach (ParticleSystem particleSystem in m_particleSystems)
                    if (particleSystem.IsAlive(false)) return true;
                foreach (VisualEffect visualEffect in m_visualEffects)
                    if (visualEffect.HasAnySystemAwake()) return true;
                return false;
            }
        }

        private void Awake()
        {
            m_particleSystems = GetComponentsInChildren<ParticleSystem>(true);
            m_visualEffects = GetComponentsInChildren<VisualEffect>(true);
            foreach (ParticleSystem particleSystem in m_particleSystems)
            {
                ParticleSystem.MainModule main = particleSystem.main;
                main.playOnAwake = false;
            }
        }

        /// <summary>Plays at the position and rotation, or following the target's when one is given.</summary>
        public VfxHandle Play(Vector3 position, Quaternion rotation, Transform target)
        {
            m_target = target;
            m_stopping = false;
            m_startFrame = Time.frameCount;
            if (target) transform.SetPositionAndRotation(target.position, target.rotation);
            else transform.SetPositionAndRotation(position, rotation);
            foreach (ParticleSystem particleSystem in m_particleSystems)
                particleSystem.Play(false);
            foreach (VisualEffect visualEffect in m_visualEffects)
                visualEffect.Reinit();
            return new VfxHandle(this, m_playId);
        }

        public bool IsCurrent(int playId) => playId == m_playId && !IsDisposed && !m_stopping;

        public void Stop(int playId)
        {
            if (!IsCurrent(playId)) return;
            m_stopping = true;
            foreach (ParticleSystem particleSystem in m_particleSystems)
                particleSystem.Stop(false, ParticleSystemStopBehavior.StopEmitting);
            foreach (VisualEffect visualEffect in m_visualEffects)
                visualEffect.Stop();
        }

        public void OnRequested() { }

        public void OnDisposed()
        {
            foreach (ParticleSystem particleSystem in m_particleSystems)
                particleSystem.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            foreach (VisualEffect visualEffect in m_visualEffects)
                visualEffect.Stop();
            m_target = null;
            m_playId++;
        }

        private void Update()
        {
            if (m_target) transform.SetPositionAndRotation(m_target.position, m_target.rotation);
            if (Time.frameCount - m_startFrame >= StartupFrames && !IsAlive) ParentPool?.ReturnToPool(this);
        }
    }
}
