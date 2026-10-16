#if UNITY_6000_6_OR_NEWER
using System;
using RogueLikeEngine.Systems.Brains;
using RogueLikeEngine.Systems.Brains.Nodes;
using RogueLikeEngine.Systems.Entities;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace RogueLikeEngine.EditorTools.Brains
{
    [Serializable, Node("Brain/Actions", "", "Find Target")]
    public sealed class FindTargetNode : BrainGraphNode
    {
        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption<LayerMask>("Layers").WithDefaultValue((LayerMask)(-1)).Build();
            context.AddOption<bool>("Use 2D").Build();
        }

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            base.OnDefinePorts(context);
            context.AddInputPort<Entity>("Store in").Build();
            context.AddInputPort<float>("Radius").WithDefaultValue(15).Build();
        }

        public override BrainNode Build(BrainGraphCompiler compiler)
        {
            GetNodeOptionByName("Layers").TryGetValue(out LayerMask layers);
            GetNodeOptionByName("Use 2D").TryGetValue(out bool use2D);
            return new FindTarget
            {
                TargetVariable = compiler.Variable<Entity>(GetInputPortByName("Store in")),
                Radius = compiler.Value<float>(GetInputPortByName("Radius")),
                Layers = layers,
                Use2D = use2D
            };
        }
    }
}
#endif
