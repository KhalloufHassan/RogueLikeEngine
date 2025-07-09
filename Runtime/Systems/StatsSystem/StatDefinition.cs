using UnityEngine;

namespace RogueLikeEngine.Systems.Stats
{
    [CreateAssetMenu(fileName = nameof(StatDefinition), menuName = "Stats/" + nameof(StatDefinition))]

    public class StatDefinition : ScriptableObject
    {
        public Sprite icon;
        public string statName;
    }
}
