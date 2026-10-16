#if UNITY_6000_6_OR_NEWER
using System;
using RogueLikeEngine.Systems.Brains;
using RogueLikeEngine.Systems.Brains.Nodes;
using Unity.GraphToolkit.Editor;

namespace RogueLikeEngine.EditorTools.Brains
{
    [Serializable, Node("Brain/Actions", "", "Wait")]
    public sealed class WaitNode : BrainGraphNode
    {
        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            base.OnDefinePorts(context);
            context.AddInputPort<float>("Duration").WithDefaultValue(1).Build();
        }

        public override BrainNode Build(BrainGraphCompiler compiler) => new Wait
        {
            Duration = compiler.Value<float>(GetInputPortByName("Duration"))
        };
    }
}
#endif
