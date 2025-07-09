using System;
using Random = UnityEngine.Random;

namespace RogueLikeEngine.Utils.Randomizers
{
    [Serializable]

    public struct RandomFloatRange
    {
        public float min;
        public float max;

        public float GetRandom()
        {
            return Random.Range(min, max);
        }
    }
}