using System;
using RogueLikeEngine.Attributes;
using RogueLikeEngine.Audio;
using RogueLikeEngine.Vfx;
using UnityEngine;

namespace RogueLikeEngine.Surfaces
{
    /// <summary>The sound and VFX of one surface in a <see cref="SurfaceEffects"/> table.</summary>
    [Serializable]
    public class SurfaceEffect
    {
        [Tooltip("Leave empty for the default effect, used for surfaces the table doesn't list.")]
        [SerializeField] private SurfaceDefinition m_surface;
        [SerializeReference, ReferencePicker] private ISound m_sound;
        [SerializeField] private PooledVfx m_vfx;
        [SerializeField] private SurfaceEffectAlignment m_alignment;

        public SurfaceDefinition Surface => m_surface;
        public ISound Sound => m_sound;
        public PooledVfx Vfx => m_vfx;
        public SurfaceEffectAlignment Alignment => m_alignment;
    }
}
