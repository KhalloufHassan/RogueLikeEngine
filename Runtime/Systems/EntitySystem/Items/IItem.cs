using System.Collections.Generic;
using RogueLikeEngine.Systems.Entities.Effects;
using RogueLikeEngine.Systems.Stats;
using UnityEngine;

namespace RogueLikeEngine.Systems.Entities.Items
{
    public interface IItem
    {
        string Name { get; }
        string Description { get; }
        Sprite Icon { get; }
        IEnumerable<IStatModifier> Modifiers { get; }
        IEnumerable<IEffect> Effects { get; }
        
        IItem GetCopy();
    }
}