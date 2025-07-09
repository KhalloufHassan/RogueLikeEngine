using RogueLikeEngine.Attributes;
using RogueLikeEngine.Optimization.Pooling;
using RogueLikeEngine.Systems.Stats;
using UnityEngine;

namespace RogueLikeEngine.Systems.Weapons
{
    [CreateAssetMenu(fileName = nameof(WeaponData), menuName = "Weapons/" + nameof(WeaponData))]
    public class WeaponData : ScriptableObject
    {
        public StatDefinition damageStat;
        public string weaponName;
        public string description;
        public Sprite icon;
        public int baseDamage;
        public float baseFireRate;
        public float baseRange;
        public ProjectilesPool projectilesPool;
        [HideIf(nameof(projectilesPool))]
        public Projectile projectilePrefab;

        public WeaponInstance CreateWeaponInstance() => new(this);
    }
}
