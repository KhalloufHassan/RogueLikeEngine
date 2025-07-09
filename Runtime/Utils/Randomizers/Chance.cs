using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace RogueLikeEngine.Utils.Randomizers
{
    [Serializable]

    public class Chance
    {
        [Range(0, 1)] public float chance;
        public bool Roll => Random.value < chance;

        public Chance()
        {
        }

        public Chance(float chance)
        {
            this.chance = chance;
        }

        public static implicit operator Chance(float floatValue) => new(floatValue);

    }
}