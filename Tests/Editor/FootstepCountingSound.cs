using RogueLikeEngine.Audio;

namespace RogueLikeEngine.Tests
{
    public class FootstepCountingSound : ISound
    {
        public int Plays { get; private set; }
        public AudioMixerType Channel => AudioMixerType.SFX;
        public bool IgnoreTimeScale => false;
        public float Delay => 0;
        public float Duration => 0;

        public ISoundPlayback CreatePlayback()
        {
            Plays++;
            return null;
        }
    }
}
