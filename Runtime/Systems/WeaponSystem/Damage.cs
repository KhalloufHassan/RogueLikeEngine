using System;

namespace RogueLikeEngine.Systems.Weapons
{
    [Serializable]
    public struct Damage
    {
        public int Value;

        public Damage(int value)
        {
            Value = value;
        }
    }
}