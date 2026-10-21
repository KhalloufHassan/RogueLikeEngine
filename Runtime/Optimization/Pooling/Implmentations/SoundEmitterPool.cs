using RogueLikeEngine.Audio;
using UnityEngine;

namespace RogueLikeEngine.Optimization.Pooling
{
    [CreateAssetMenu(menuName = "Pools/" + nameof(SoundEmitterPool), fileName = nameof(SoundEmitterPool))]
    public class SoundEmitterPool : Pool<SoundEmitter>
    {
    }
}
