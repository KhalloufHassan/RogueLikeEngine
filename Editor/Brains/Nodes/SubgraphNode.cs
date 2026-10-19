#if UNITY_6000_6_OR_NEWER
using System;
using RogueLikeEngine.Systems.Brains;
using Unity.GraphToolkit.Editor;

namespace RogueLikeEngine.EditorTools.Brains
{
    /// <summary>Expands another authored brain into this tree, sharing its blackboard by variable name.</summary>
    [Serializable, Node("Brain", BrainNodeIcons.Subgraph, "Subgraph")]
    public sealed class SubgraphNode : BrainGraphNode
    {
        private const string DefaultTitle = "Subgraph";
        private const string GraphOption = "Graph";

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            base.OnDefineOptions(context);
            context.AddOption<BrainGraph>(GraphOption).Build();
        }

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            base.OnDefinePorts(context);
            GetNodeOptionByName(GraphOption).TryGetValue(out BrainGraph graph);
            if (graph && Title == DefaultTitle) Title = graph.name;
            Subtitle = Title == DefaultTitle ? string.Empty : DefaultTitle;
        }

        public override BrainNode Build(BrainGraphCompiler compiler)
        {
            GetNodeOptionByName(GraphOption).TryGetValue(out BrainGraph graph);
            return compiler.Subgraph(graph);
        }
    }
}
#endif
