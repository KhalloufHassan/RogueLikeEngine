#if UNITY_6000_6_OR_NEWER
using System;
using RogueLikeEngine.Systems.Brains;
using RogueLikeEngine.Systems.Brains.Nodes;
using RogueLikeEngine.Systems.Entities;
using Unity.GraphToolkit.Editor;

namespace RogueLikeEngine.EditorTools.Brains
{
    [Serializable, Node("Brain/Actions", BrainNodeIcons.Movement, "Chase Target")]
    public sealed class ChaseTargetNode : BrainGraphNode
    {
        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            base.OnDefinePorts(context);
            context.AddInputPort<Entity>("Target").Build();
            context.AddInputPort<float>("Stop distance").WithDefaultValue(2).Build();
        }

        public override BrainNode Build(BrainGraphCompiler compiler) => new ChaseTarget
        {
            Target = compiler.Value<Entity>(GetInputPortByName("Target")),
            StopDistance = compiler.Value<float>(GetInputPortByName("Stop distance"))
        };
    }
}
#endif
