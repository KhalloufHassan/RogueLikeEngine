using System;
using UnityEngine;

namespace RogueLikeEngine.Systems.Stats
{
    [Serializable]
    public class BasicStatModifier : AbstractStatModifier
    {
        
        [SerializeField] protected float m_value;

        public override float Value => m_value;
        public override float ValuePreview => m_value;

        public void ForceNewValue(float value) => m_value = value;
    }
}