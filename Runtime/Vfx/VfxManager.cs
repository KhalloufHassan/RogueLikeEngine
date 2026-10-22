using System;
using System.Collections.Generic;
using RogueLikeEngine.Optimization.Pooling;
using UnityEngine;

namespace RogueLikeEngine.Vfx
{
    /// <summary>
    /// Plays <see cref="PooledVfx"/> prefabs, each from its own pool created on first use, so effects need no pool assets.
    /// Used as an asset referenced by whoever plays effects, there is no scene instance.
    /// </summary>
    [CreateAssetMenu(fileName = nameof(VfxManager), menuName = "Vfx/" + nameof(VfxManager))]
    public class VfxManager : ScriptableObject
    {
        [NonSerialized] private readonly Dictionary<PooledVfx, VfxPool> m_pools = new();

        public VfxHandle Play(PooledVfx prefab, Vector3 position, Quaternion rotation) => PlayEffect(prefab, position, rotation, null);

        /// <summary>Plays following the target's position and rotation.</summary>
        public VfxHandle Play(PooledVfx prefab, Transform target) => PlayEffect(prefab, Vector3.zero, Quaternion.identity, target);

        private VfxHandle PlayEffect(PooledVfx prefab, Vector3 position, Quaternion rotation, Transform target) =>
            prefab ? PoolOf(prefab).Request().Play(position, rotation, target) : default;

        private VfxPool PoolOf(PooledVfx prefab)
        {
            if (m_pools.TryGetValue(prefab, out VfxPool pool) && pool) return pool;
            pool = CreateInstance<VfxPool>();
            pool.hideFlags = HideFlags.DontUnloadUnusedAsset;
            pool.prefab = prefab;
            return m_pools[prefab] = pool;
        }
    }
}
