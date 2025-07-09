using System.Collections.Generic;
using RogueLikeEngine.Attributes;
using RogueLikeEngine.Systems.Entities.Effects;
using RogueLikeEngine.Systems.Stats;
using UnityEngine;

namespace RogueLikeEngine.Systems.Entities.Items
{
    [CreateAssetMenu(fileName = nameof(ItemScriptableObject), menuName = "Items/" + nameof(ItemScriptableObject))]
    public class ItemScriptableObject : ScriptableObject,IItem
    {
        public string itemName;
        public string description;
        public Sprite icon;
        [SerializeReference, ReferencePicker]
        public IStatModifier[] modifiers;
        public EffectScriptableObject[] effects;

        public string Name => itemName;
        public string Description => description;
        public Sprite Icon => icon;
        public IEnumerable<IStatModifier> Modifiers => modifiers;
        public IEnumerable<IEffect> Effects => effects;

        public IItem GetCopy() => Instantiate(this);
    }
}
