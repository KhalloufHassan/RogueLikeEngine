#if UNITY_6000_6_OR_NEWER
using System;
using RogueLikeEngine.Systems.Brains;
using RogueLikeEngine.Systems.Brains.Nodes;
using RogueLikeEngine.Systems.Entities;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace RogueLikeEngine.EditorTools.Brains
{
    [Serializable, Node("Brain/Actions", BrainNodeIcons.Movement, "MoveToPosition")]
    public sealed class MoveToPositionNode : BrainGraphNode
    {
        private const string DestinationPort = "Destination";
        private const string StopDistancePort = "StopDistance";
        private const string TimeoutPort = "Timeout";

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            base.OnDefinePorts(context);
            context.AddInputPort<Vector3>(DestinationPort).Build();
            context.AddInputPort<float>(StopDistancePort).WithDefaultValue(.2f).Build();
            context.AddInputPort<float>(TimeoutPort).WithDefaultValue(6).Build();
        }

        public override BrainNode Build(BrainGraphCompiler compiler)
        {
            return new MoveToPosition
            {
                Destination = compiler.Value<Vector3>(GetInputPortByName(DestinationPort)),
                StopDistance = compiler.Value<float>(GetInputPortByName(StopDistancePort)),
                Timeout = compiler.Value<float>(GetInputPortByName(TimeoutPort))
            };
        }
    }
}
#endif
