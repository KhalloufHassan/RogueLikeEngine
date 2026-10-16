#if UNITY_6000_6_OR_NEWER
using System;
using RogueLikeEngine.Systems.Brains;
using Unity.GraphToolkit.Editor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace RogueLikeEngine.EditorTools.Brains
{
    [ScriptedImporter(1, BrainAuthoringGraph.Extension)]
    public sealed class BrainGraphImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext context)
        {
            BrainGraph asset = ScriptableObject.CreateInstance<BrainGraph>();
            context.AddObjectToAsset("Brain", asset);
            context.SetMainObject(asset);
            try
            {
                BrainAuthoringGraph graph = GraphDatabase.LoadGraphForImporter<BrainAuthoringGraph>(context.assetPath);
                if (graph.NodeCount == 0) return;
                BrainGraphCompiler compiler = new(graph);
                BrainVariable[] variables = compiler.Variables();
                asset.SetDefinition(compiler.Root(), variables);
            }
            catch (InvalidOperationException exception)
            {
                context.LogImportError(exception.Message);
            }
        }
    }
}
#endif
