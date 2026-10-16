#if UNITY_6000_6_OR_NEWER
using System;
using Unity.GraphToolkit.Editor;

namespace RogueLikeEngine.EditorTools.Brains
{
    [Serializable, UseWithGraph(typeof(BrainAuthoringGraph)), Node("Brain", "", "Entry")]
    public sealed class EntryNode : Node
    {
        protected override void OnDefinePorts(IPortDefinitionContext context) =>
            context.AddOutputPort("Root").WithCapacity(PortCapacity.Single).Build();
    }
}
#endif
