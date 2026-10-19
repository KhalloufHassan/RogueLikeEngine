#if UNITY_6000_6_OR_NEWER
using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace RogueLikeEngine.EditorTools.Brains
{
    [Serializable]
    public abstract class CompositeNode : BrainGraphNode
    {
        private const string ChildrenOption = "Children";
        private const string ChildPrefix = "Child ";
        private const string NameSuffix = " name";

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            base.OnDefineOptions(context);
            context.AddOption<int>(ChildrenOption).WithDefaultValue(2).Build().TryGetValue(out int count);
            for (int i = 0; i < Mathf.Clamp(count, 1, 32); i++)
                context.AddOption<string>(ChildPrefix + (i + 1) + NameSuffix).Delayed().ShowInInspectorOnly().Build();
        }

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            base.OnDefinePorts(context);
            GetNodeOptionByName(ChildrenOption).TryGetValue(out int count);
            for (int i = 0; i < Mathf.Clamp(count, 1, 32); i++)
            {
                string port = ChildPrefix + (i + 1);
                GetNodeOptionByName(port + NameSuffix).TryGetValue(out string name);
                context.AddOutputPort(port).WithDisplayName(string.IsNullOrWhiteSpace(name) ? port : $"{i + 1}. {name}")
                    .WithCapacity(PortCapacity.Single).Build();
            }
        }
    }
}
#endif
