#if UNITY_6000_6_OR_NEWER
using System;
using RogueLikeEngine.Systems.Brains;
using Unity.GraphToolkit.Editor;

namespace RogueLikeEngine.EditorTools.Brains
{
    /// <summary>Extend this in an Editor assembly and build a serializable runtime BrainNode.</summary>
    [Serializable, UseWithGraph(typeof(BrainAuthoringGraph))]
    public abstract class BrainGraphNode : Node
    {
        protected override void OnDefinePorts(IPortDefinitionContext context) =>
            context.AddInputPort("In").WithCapacity(PortCapacity.Single).Build();

        public abstract BrainNode Build(BrainGraphCompiler compiler);
    }
}
#endif
