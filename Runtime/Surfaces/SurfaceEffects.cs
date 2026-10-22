using RogueLikeEngine.Audio;
using RogueLikeEngine.Vfx;
using UnityEngine;

namespace RogueLikeEngine.Surfaces
{
    /// <summary>
    /// What something produces on each surface, owned by whatever hits or touches it: a projectile's impacts, a
    /// character's footsteps. Exact surface matches are searched through the fallback chain before any default.
    /// </summary>
    [CreateAssetMenu(fileName = nameof(SurfaceEffects), menuName = "Surfaces/" + nameof(SurfaceEffects))]
    public class SurfaceEffects : ScriptableObject
    {
        private const int MaxFallbackDepth = 8;

        [SerializeField] private SoundManager m_soundManager;
        [SerializeField] private VfxManager m_vfxManager;
        [Tooltip("Effects per surface. An entry without a surface is the default, used for surfaces not listed.")]
        [SerializeField] private SurfaceEffect[] m_effects;
        [Tooltip("Searched for surfaces this table doesn't list, so tables can share and override a common one.")]
        [SerializeField] private SurfaceEffects m_fallback;

        /// <summary>The effect for the surface, the default when it isn't listed, or null when there is none.</summary>
        public SurfaceEffect Get(SurfaceDefinition surface) => (surface ? Find(surface, 0) : null) ?? Find(null, 0);

        /// <summary>
        /// Plays the surface's effect at the point. <paramref name="direction"/> is where the hitter was moving, used by
        /// effects aligned against it.
        /// </summary>
        public void Play(SurfaceDefinition surface, Vector3 point, Vector3 normal, Vector3 direction)
        {
            SurfaceEffect effect = Get(surface);
            if (effect == null) return;
            if (effect.Sound != null && m_soundManager) m_soundManager.Play(effect.Sound, point);
            if (!effect.Vfx || !m_vfxManager) return;
            Vector3 forward = effect.Alignment == SurfaceEffectAlignment.SurfaceNormal ? normal : -direction;
            m_vfxManager.Play(effect.Vfx, point, forward.sqrMagnitude > 0 ? Quaternion.LookRotation(forward) : Quaternion.identity);
        }

        private SurfaceEffect Find(SurfaceDefinition surface, int depth)
        {
            if (m_effects != null)
            {
                foreach (SurfaceEffect effect in m_effects)
                    if (effect.Surface == surface) return effect;
            }

            return m_fallback && depth < MaxFallbackDepth ? m_fallback.Find(surface, depth + 1) : null;
        }
    }
}
