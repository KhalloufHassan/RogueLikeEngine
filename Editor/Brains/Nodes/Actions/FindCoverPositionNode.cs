#if UNITY_6000_6_OR_NEWER
using System;
using RogueLikeEngine.Systems.Brains;
using RogueLikeEngine.Systems.Brains.Nodes;
using RogueLikeEngine.Systems.Entities;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace RogueLikeEngine.EditorTools.Brains
{
    [Serializable, Node("Brain/Actions", BrainNodeIcons.Search, "FindCoverPosition")]
    public sealed class FindCoverPositionNode : BrainGraphNode
    {
        private const string TargetPort = "Target";
        private const string RadiusPort = "Radius";
        private const string PreferredDistancePort = "PreferredDistance";
        private const string DestinationPort = "Destination";
        private const string AgentOption = "Agent type ID";
        private const string LayersOption = "Layers";

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            base.OnDefineOptions(context);
            context.AddOption<int>(AgentOption).Build();
            context.AddOption<LayerMask>(LayersOption).WithDefaultValue((LayerMask)(-1)).Build();
        }

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            base.OnDefinePorts(context);
            context.AddInputPort<Entity>(TargetPort).Build();
            context.AddInputPort<float>(RadiusPort).WithDefaultValue(15).Build();
            context.AddInputPort<float>(PreferredDistancePort).WithDefaultValue(8).Build();
            context.AddInputPort<Vector3>(DestinationPort).Build();
        }

        public override BrainNode Build(BrainGraphCompiler compiler)
        {
            GetNodeOptionByName(AgentOption).TryGetValue(out int agentType);
            GetNodeOptionByName(LayersOption).TryGetValue(out LayerMask layers);
            return new FindCoverPosition
            {
                Target = compiler.Value<Entity>(GetInputPortByName(TargetPort)),
                Radius = compiler.Value<float>(GetInputPortByName(RadiusPort)),
                PreferredDistance = compiler.Value<float>(GetInputPortByName(PreferredDistancePort)),
                DestinationVariable = compiler.Variable<Vector3>(GetInputPortByName(DestinationPort)),
                AgentTypeID = agentType,
                Layers = layers
            };
        }
    }
}
#endif
