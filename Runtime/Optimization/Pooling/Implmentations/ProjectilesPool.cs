using RogueLikeEngine.Systems.Weapons;
using UnityEngine;

namespace RogueLikeEngine.Optimization.Pooling
{
    [CreateAssetMenu(menuName = "Pools/" + nameof(ProjectilesPool), fileName = nameof(ProjectilesPool))]

    public class ProjectilesPool : Pool<Projectile>
    {
    }
}