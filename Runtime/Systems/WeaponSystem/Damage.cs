using System;
using UnityEngine;

namespace RogueLikeEngine.Systems.Weapons
{
    [Serializable]
    public struct Damage
    {
        public int Value;
        public Vector3 Direction;

        public Damage(int value, Vector3 direction = default)
        {
            Value = value;
            Direction = direction;
        }
    }
}
