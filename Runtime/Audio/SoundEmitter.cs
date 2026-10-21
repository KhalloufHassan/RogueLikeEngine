using System.Collections.Generic;
using RogueLikeEngine.Optimization.Pooling;
using UnityEngine;
using UnityEngine.Audio;

namespace RogueLikeEngine.Audio
{
    /// <summary>
    /// Pooled host for one <see cref="ISoundPlayback"/> at a time. Follows an optional target, handles delay, duration
    /// limit, time scale, handle multipliers and a short fade when stopped, then returns to its pool. Each play gets a new
    /// id, so stale <see cref="SoundHandle"/>s can't affect the next sound.
    /// </summary>
    public class SoundEmitter : MonoBehaviour, IPoolObject
    {
        private const float StopFadeDuration = 0.02f;

        [SerializeField] private AudioSource m_audioSource;

        private readonly List<AudioSource> m_sources = new();
        private ISoundPlayback m_playback;
        private Transform m_target;
        private bool m_ignoreTimeScale;
        private bool m_started;
        private bool m_stopping;
        private int m_playId;
        private float m_delay;
        private float m_duration;
        private float m_elapsed;
        private float m_stopFade;
        private float m_volumeMultiplier;

        public IPool ParentPool { get; set; }
        public bool IsDisposed { get; set; }

        public AudioSource Source => m_audioSource;
        public float PitchMultiplier { get; private set; }
        /// <summary>The handle's volume multiplier, including the stop fade.</summary>
        public float VolumeMultiplier => m_stopping ? m_volumeMultiplier * Mathf.Max(0, m_stopFade) / StopFadeDuration : m_volumeMultiplier;
        public float TimeScale => m_ignoreTimeScale ? 1 : Time.timeScale;
        /// <summary>The handle's 0 to 1 intensity, interpreted by playbacks that react to one (such as a charge).</summary>
        public float Intensity { get; private set; }

        /// <summary>
        /// Returns the source at <paramref name="index"/>, the prefab's own source being 0. Extra sources are created
        /// once with the same spatial settings, for playbacks that need overlapping clips.
        /// </summary>
        public AudioSource GetSource(int index)
        {
            if (m_sources.Count == 0) m_sources.Add(m_audioSource);
            while (m_sources.Count <= index) m_sources.Add(CreateSource());
            return m_sources[index];
        }

        /// <summary>
        /// Starts the playback after <paramref name="delay"/>. A positive duration stops it after that many seconds.
        /// Delay, duration and pitch follow the time scale unless <paramref name="ignoreTimeScale"/> is set.
        /// </summary>
        public SoundHandle Play(ISoundPlayback playback, AudioMixerGroup group, Vector3 position, Transform target, float delay, float duration, bool ignoreTimeScale)
        {
            m_playback = playback;
            m_target = target;
            m_ignoreTimeScale = ignoreTimeScale;
            m_delay = delay;
            m_duration = duration > 0 ? duration : float.PositiveInfinity;
            m_elapsed = 0;
            m_started = false;
            m_stopping = false;
            PitchMultiplier = 1;
            m_volumeMultiplier = 1;
            Intensity = 0;
            transform.position = target ? target.position : position;
            GetSource(0);
            foreach (AudioSource source in m_sources)
                source.outputAudioMixerGroup = group;
            playback.Handle = new SoundHandle(this, m_playId);
            if (delay <= 0) Begin();
            return playback.Handle;
        }

        public bool IsCurrent(int playId) => playId == m_playId && !IsDisposed && !m_stopping;

        public void SetPitch(int playId, float multiplier)
        {
            if (IsCurrent(playId)) PitchMultiplier = multiplier;
        }

        public void SetVolume(int playId, float multiplier)
        {
            if (IsCurrent(playId)) m_volumeMultiplier = multiplier;
        }

        public void SetIntensity(int playId, float intensity)
        {
            if (IsCurrent(playId)) Intensity = Mathf.Clamp01(intensity);
        }

        public void Release(int playId)
        {
            if (!IsCurrent(playId)) return;
            if (m_started) m_playback.Release(this);
            else Stop(playId);
        }

        public void Stop(int playId)
        {
            if (!IsCurrent(playId)) return;
            m_stopping = true;
            m_stopFade = StopFadeDuration;
        }

        public void OnRequested() { }

        public void OnDisposed()
        {
            foreach (AudioSource source in m_sources)
            {
                source.Stop();
                source.clip = null;
            }

            m_playback?.Recycle();
            m_playback = null;
            m_playId++;
        }

        private void Begin()
        {
            m_started = true;
            m_playback.Start(this);
        }

        private AudioSource CreateSource()
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.outputAudioMixerGroup = m_audioSource.outputAudioMixerGroup;
            source.rolloffMode = m_audioSource.rolloffMode;
            source.minDistance = m_audioSource.minDistance;
            source.maxDistance = m_audioSource.maxDistance;
            source.dopplerLevel = m_audioSource.dopplerLevel;
            source.spread = m_audioSource.spread;
            source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, m_audioSource.GetCustomCurve(AudioSourceCurveType.CustomRolloff));
            return source;
        }

        private void Update()
        {
            if (m_target) transform.position = m_target.position;
            float deltaTime = m_ignoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime;
            if (!m_started)
            {
                if (m_stopping) ParentPool.ReturnToPool(this);
                else if ((m_delay -= deltaTime) <= 0) Begin();
                return;
            }

            if (m_stopping) m_stopFade -= Time.unscaledDeltaTime;
            else if ((m_elapsed += deltaTime) >= m_duration) Stop(m_playId);
            if (!m_playback.Update(this, deltaTime) || m_stopping && m_stopFade <= 0)
                ParentPool.ReturnToPool(this);
        }
    }
}
