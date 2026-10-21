using System;
using UnityEngine;
using RogueLikeEngine.Optimization.Pooling;
using UnityEngine.Audio;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RogueLikeEngine.Audio
{
    /// <summary>
    /// Plays <see cref="ISound"/>s on pooled <see cref="SoundEmitter"/>s and owns the persisted mixer channel volumes.
    /// Used as an asset referenced by whoever plays sounds, there is no scene instance. The mixer is optional;
    /// when assigned it must expose the channel volume parameters named below.
    /// </summary>
    [CreateAssetMenu(fileName = nameof(SoundManager), menuName = "Audio/" + nameof(SoundManager))]
    public class SoundManager : ScriptableObject
    {
        private const string MasterVolumeParameter = "MasterVolume";
        private const string SfxVolumeParameter = "SfxVolume";
        private const string UIVolumeParameter = "UIVolume";
        private const string MusicVolumeParameter = "MusicVolume";
        private const float MutedDecibels = -80f;

        [SerializeField] private SoundEmitterPool m_pool;
        [SerializeField] private AudioMixer m_audioMixer;
        [SerializeField] private AudioMixerGroup m_masterGroup;
        [SerializeField] private AudioMixerGroup m_sfxGroup;
        [SerializeField] private AudioMixerGroup m_uiGroup;
        [SerializeField] private AudioMixerGroup m_musicGroup;

        [NonSerialized] private bool m_volumesLoaded;

        /// <summary>
        /// Plays the sound following <paramref name="target"/> (at the origin when null). Returns an inactive handle when the
        /// sound is skipped.
        /// </summary>
        public SoundHandle Play(ISound sound, Transform target = null) => PlaySound(sound, Vector3.zero, target);

        /// <inheritdoc cref="Play(ISound, Transform)"/>
        public SoundHandle Play(ISound sound, Vector3 position) => PlaySound(sound, position, null);

        public static float GetVolume(AudioMixerType channel) => PlayerPrefs.GetFloat(channel.ToString(), 1);

        public void SetVolume(AudioMixerType channel, float value)
        {
            PlayerPrefs.SetFloat(channel.ToString(), value);
            ApplyVolume(channel, value);
        }

        private SoundHandle PlaySound(ISound sound, Vector3 position, Transform target)
        {
            ISoundPlayback playback = sound?.CreatePlayback();
            if (playback == null) return default;
            if (!m_volumesLoaded) LoadVolumes();
            return m_pool.Request().Play(playback, GetGroup(sound.Channel), position, target, sound.Delay, sound.Duration, sound.IgnoreTimeScale);
        }

        private void LoadVolumes()
        {
            foreach (AudioMixerType channel in Enum.GetValues(typeof(AudioMixerType)))
                ApplyVolume(channel, GetVolume(channel));
            m_volumesLoaded = true;
        }

        private void ApplyVolume(AudioMixerType channel, float value)
        {
            if (m_audioMixer)
                m_audioMixer.SetFloat(GetVolumeParameter(channel), value > 0.0001f ? Mathf.Log10(value) * 20f : MutedDecibels);
        }

        private AudioMixerGroup GetGroup(AudioMixerType channel) => channel switch
        {
            AudioMixerType.SFX => m_sfxGroup,
            AudioMixerType.UI => m_uiGroup,
            AudioMixerType.Music => m_musicGroup,
            _ => m_masterGroup
        };

        private static string GetVolumeParameter(AudioMixerType channel) => channel switch
        {
            AudioMixerType.SFX => SfxVolumeParameter,
            AudioMixerType.UI => UIVolumeParameter,
            AudioMixerType.Music => MusicVolumeParameter,
            _ => MasterVolumeParameter
        };

#if UNITY_EDITOR
        private void OnEnable() => EditorApplication.playModeStateChanged += ResetOnPlayModeExit;

        private void OnDisable() => EditorApplication.playModeStateChanged -= ResetOnPlayModeExit;

        private void ResetOnPlayModeExit(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingPlayMode) m_volumesLoaded = false;
        }
#endif
    }
}
