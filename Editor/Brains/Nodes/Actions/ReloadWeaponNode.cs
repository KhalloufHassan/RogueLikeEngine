#if UNITY_6000_6_OR_NEWER
using System;
using RogueLikeEngine.Systems.Brains;
using RogueLikeEngine.Systems.Brains.Nodes;
using Unity.GraphToolkit.Editor;

namespace RogueLikeEngine.EditorTools.Brains
{
    [Serializable, Node("Brain/Actions", "", "Reload Weapon")]
    public sealed class ReloadWeaponNode : BrainGraphNode
    {
        public override BrainNode Build(BrainGraphCompiler compiler) => new ReloadWeapon();
    }
}
#endif
