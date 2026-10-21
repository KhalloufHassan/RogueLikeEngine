namespace RogueLikeEngine.Audio
{
    /// <summary>
    /// Inspector-authored sound played through <see cref="SoundManager"/>. Each implementation decides how it plays
    /// through the <see cref="ISoundPlayback"/> it creates. Use <c>[SerializeReference, ReferencePicker]</c> fields to pick
    /// the sound type in the Inspector.
    /// </summary>
    public interface ISound
    {
        AudioMixerType Channel { get; }
        bool IgnoreTimeScale { get; }
        /// <summary>Seconds to wait before playing.</summary>
        float Delay { get; }
        /// <summary>Seconds after which the sound is stopped, zero lets it end on its own or when stopped.</summary>
        float Duration { get; }

        /// <summary>Creates the state for one playback, or returns null when the sound can't play now.</summary>
        ISoundPlayback CreatePlayback();
    }
}
