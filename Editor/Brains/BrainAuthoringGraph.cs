#if UNITY_6000_6_OR_NEWER
using System;
using Unity.GraphToolkit.Editor;
using UnityEditor;

namespace RogueLikeEngine.EditorTools.Brains
{
    [Serializable, Graph(Extension, GraphOptions.DisableAutoInclusionOfNodesFromGraphAssembly)]
    public sealed class BrainAuthoringGraph : Graph
    {
        public const string Extension = "rlebrain";

        [MenuItem("Assets/Create/Rogue Like Engine/Brain Graph")]
        private static void CreateAsset() => GraphDatabase.PromptInProjectBrowserToCreateNewAsset<BrainAuthoringGraph>();

        public override void OnGraphChanged(GraphLogger logger)
        {
            base.OnGraphChanged(logger);
            try
            {
                new BrainGraphCompiler(this).Validate();
            }
            catch (InvalidOperationException exception)
            {
                logger.LogError(exception.Message);
            }
        }
    }
}
#endif
