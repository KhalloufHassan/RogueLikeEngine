using System;
using Random = UnityEngine.Random;

namespace RogueLikeEngine.Utils.Randomizers
{
    [Serializable]
    public struct RandomIntRange

    {
        public int min;
        public int max;

        public int GetRandom()
        {
            return Random.Range(min, max+1);
        }
    }
}
