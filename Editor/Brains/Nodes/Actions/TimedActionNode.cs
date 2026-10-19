#if UNITY_6000_6_OR_NEWER
using System;
using RogueLikeEngine.Systems.Brains;
using RogueLikeEngine.Systems.Brains.Nodes;
using RogueLikeEngine.Systems.Entities;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace RogueLikeEngine.EditorTools.Brains
{
    [Serializable, Node("Brain/Actions", BrainNodeIcons.Time, "TimedAction")]
    public sealed class TimedActionNode : BrainGraphNode
    {
        private const string DurationPort = "Duration";
        private const string ChildPort = "Child";

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            base.OnDefinePorts(context);
            context.AddInputPort<float>(DurationPort).WithDefaultValue(2).Build();
            context.AddOutputPort(ChildPort).WithCapacity(PortCapacity.Single).Build();
        }

        public override BrainNode Build(BrainGraphCompiler compiler)
        {
            return new TimedAction
            {
                Duration = compiler.Value<float>(GetInputPortByName(DurationPort)),
                Child = compiler.Children(this)[0]
            };
        }
    }
}
#endif
