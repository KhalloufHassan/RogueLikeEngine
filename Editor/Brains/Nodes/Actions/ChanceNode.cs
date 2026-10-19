#if UNITY_6000_6_OR_NEWER
using System;
using RogueLikeEngine.Systems.Brains;
using RogueLikeEngine.Systems.Brains.Nodes;
using RogueLikeEngine.Systems.Entities;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace RogueLikeEngine.EditorTools.Brains
{
    [Serializable, Node("Brain/Actions", BrainNodeIcons.Chance, "Chance")]
    public sealed class ChanceNode : BrainGraphNode
    {
        private const string ProbabilityPort = "Probability";

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            base.OnDefinePorts(context);
            context.AddInputPort<float>(ProbabilityPort).WithDefaultValue(.5f).Build();
        }

        public override BrainNode Build(BrainGraphCompiler compiler)
        {
            return new Chance
            {
                Probability = compiler.Value<float>(GetInputPortByName(ProbabilityPort))
            };
        }
    }
}
#endif
