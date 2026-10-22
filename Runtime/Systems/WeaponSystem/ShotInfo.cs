namespace RogueLikeEngine.Systems.Weapons
{
    /// <summary>The values a shot is fired with, already scaled by the owner's stats and charge.</summary>
    public struct ShotInfo
    {
        public WeaponInstance Weapon;
        /// <summary>Charge from 0 to 1, always 0 for weapons that aren't chargeable.</summary>
        public float Charge;
        public int ProjectileCount;
        public float SpreadAngle;
        public float Inaccuracy;
        public float DamageMultiplier;
    }
}
