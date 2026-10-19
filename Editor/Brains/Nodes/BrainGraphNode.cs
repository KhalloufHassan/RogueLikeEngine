#if UNITY_6000_6_OR_NEWER
using System;
using System.Reflection;
using RogueLikeEngine.Systems.Brains;
using Unity.GraphToolkit.Editor;

namespace RogueLikeEngine.EditorTools.Brains
{
    /// <summary>Extend this in an Editor assembly and build a serializable runtime BrainNode.</summary>
    [Serializable, UseWithGraph(typeof(BrainAuthoringGraph))]
    public abstract class BrainGraphNode : Node
    {
        private const string TitleOption = "Title";
        private const string InputPort = "In";

        protected override void OnDefineOptions(IOptionDefinitionContext context) =>
            context.AddOption<string>(TitleOption).Delayed().ShowInInspectorOnly().Build();

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            GetNodeOptionByName(TitleOption).TryGetValue(out string title);
            string nodeType = GetType().GetCustomAttribute<NodeAttribute>()?.Title;
            Title = string.IsNullOrWhiteSpace(title) ? nodeType : title;
            Subtitle = Title == nodeType ? string.Empty : nodeType;
            context.AddInputPort(InputPort).WithCapacity(PortCapacity.Single).Build();
        }

        public abstract BrainNode Build(BrainGraphCompiler compiler);
    }
}
#endif
