#if UNITY_6000_6_OR_NEWER
using System;
using RogueLikeEngine.Systems.Brains;
using RogueLikeEngine.Systems.Brains.Nodes;
using RogueLikeEngine.Systems.Entities;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace RogueLikeEngine.EditorTools.Brains
{
    [Serializable, Node("Brain/Actions", BrainNodeIcons.Search, "FindRetreatPosition")]
    public sealed class FindRetreatPositionNode : BrainGraphNode
    {
        private const string TargetPort = "Target";
        private const string SafeDistancePort = "SafeDistance";
        private const string TravelDistancePort = "TravelDistance";
        private const string DestinationPort = "Destination";
        private const string AgentOption = "Agent type ID";

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            base.OnDefineOptions(context);
            context.AddOption<int>(AgentOption).Build();
        }

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            base.OnDefinePorts(context);
            context.AddInputPort<Entity>(TargetPort).Build();
            context.AddInputPort<float>(SafeDistancePort).WithDefaultValue(6).Build();
            context.AddInputPort<float>(TravelDistancePort).WithDefaultValue(5).Build();
            context.AddInputPort<Vector3>(DestinationPort).Build();
        }

        public override BrainNode Build(BrainGraphCompiler compiler)
        {
            GetNodeOptionByName(AgentOption).TryGetValue(out int agentType);
            return new FindRetreatPosition
            {
                Target = compiler.Value<Entity>(GetInputPortByName(TargetPort)),
                SafeDistance = compiler.Value<float>(GetInputPortByName(SafeDistancePort)),
                TravelDistance = compiler.Value<float>(GetInputPortByName(TravelDistancePort)),
                DestinationVariable = compiler.Variable<Vector3>(GetInputPortByName(DestinationPort)),
                AgentTypeID = agentType
            };
        }
    }
}
#endif
