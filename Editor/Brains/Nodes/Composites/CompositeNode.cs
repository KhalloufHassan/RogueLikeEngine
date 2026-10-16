#if UNITY_6000_6_OR_NEWER
using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace RogueLikeEngine.EditorTools.Brains
{
    [Serializable]
    public abstract class CompositeNode : BrainGraphNode
    {
        protected override void OnDefineOptions(IOptionDefinitionContext context) =>
            context.AddOption<int>("Children").WithDefaultValue(2).Build();

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            base.OnDefinePorts(context);
            GetNodeOptionByName("Children").TryGetValue(out int count);
            for (int i = 0; i < Mathf.Clamp(count, 1, 32); i++)
                context.AddOutputPort($"Child {i + 1}").WithCapacity(PortCapacity.Single).Build();
        }
    }
}
#endif
