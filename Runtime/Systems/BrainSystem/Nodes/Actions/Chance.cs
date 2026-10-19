using System;
using UnityEngine;

namespace RogueLikeEngine.Systems.Brains.Nodes
{
    /// <summary>Rolls once per execution; probability is between zero and one.</summary>
    [Serializable]
    public sealed class Chance : BrainNode
    {
        public BrainValue<float> Probability;

        protected override BrainStatus OnTick(BrainContext context)
        {
            float probability = Mathf.Clamp01(Probability.Get(context));
            return probability >= 1 || probability > 0 && UnityEngine.Random.value < probability ? BrainStatus.Success : BrainStatus.Failure;
        }
    }
}
