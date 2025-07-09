using System;
using UnityEngine;

namespace RogueLikeEngine.Systems.Stats
{
    [Serializable]
    public struct ModifierTextFormatingOptions
    {
        [field: SerializeField]
        public ValueSign ValueSign { get; set; }
    }

    public enum ValueSign
    {
        FromValue,
        Positive,
        Negative,
        Neutral
    }
}