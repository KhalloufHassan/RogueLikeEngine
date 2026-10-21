namespace RogueLikeEngine.Audio
{
    /// <summary>
    /// One playback of an <see cref="ISound"/>, driven by the <see cref="SoundEmitter"/> hosting it. The emitter owns
    /// pooling, position, delay, duration limit and stopping; the playback owns everything it does with the audio sources.
    /// </summary>
    public interface ISoundPlayback
    {
        /// <summary>Set by the emitter when it takes the playback, before any delay.</summary>
        SoundHandle Handle { get; set; }

        /// <summary>Called once the delay has passed. The emitter's mixer group and position are already set.</summary>
        void Start(SoundEmitter emitter);

        /// <summary>Called every frame after <see cref="Start"/>. Returns false once the sound has finished on its own.</summary>
        bool Update(SoundEmitter emitter, float deltaTime);

        /// <summary>Ends the sound gracefully, for example a loop moving to its end part. Only called after <see cref="Start"/>.</summary>
        void Release(SoundEmitter emitter);

        /// <summary>
        /// Called once the emitter is done with this playback, finished or stopped. Implementations hand it back to their
        /// sound for reuse, so playing a sound doesn't allocate.
        /// </summary>
        void Recycle();
    }
}
