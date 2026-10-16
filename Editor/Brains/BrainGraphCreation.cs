#if UNITY_6000_6_OR_NEWER
using RogueLikeEngine.Systems.Entities;
using Unity.GraphToolkit.Editor;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;

namespace RogueLikeEngine.EditorTools.Brains
{
    public sealed class BrainGraphCreation : AssetCreationEndAction
    {
        [MenuItem("Assets/Create/Rogue Like Engine/Chasing Shooter Brain")]
        private static void CreateAsset() => ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
            default, CreateInstance<BrainGraphCreation>(), "Chasing Shooter.rlebrain", null, null);

        public override void Action(EntityId entityId, string pathName, string resourceFile)
        {
            BrainAuthoringGraph graph = GraphDatabase.CreateGraph<BrainAuthoringGraph>(pathName);
            Populate(graph);
            GraphDatabase.SaveGraph(graph);
            AssetDatabase.ImportAsset(pathName);
            ProjectWindowUtil.ShowCreatedAsset(AssetDatabase.LoadMainAssetAtPath(pathName));
        }

        public static void Populate(BrainAuthoringGraph graph)
        {
            EntryNode entry = Add<EntryNode>(graph, new Vector2(0, 0));
            SequenceNode sequence = Add<SequenceNode>(graph, new Vector2(250, 0));
            FindTargetNode find = Add<FindTargetNode>(graph, new Vector2(550, -150));
            ParallelNode parallel = Add<ParallelNode>(graph, new Vector2(550, 180));
            ChaseTargetNode chase = Add<ChaseTargetNode>(graph, new Vector2(850, 100));
            FireAtTargetNode fire = Add<FireAtTargetNode>(graph, new Vector2(850, 400));
            Connect(entry, "Root", sequence);
            Connect(sequence, "Child 1", find);
            Connect(sequence, "Child 2", parallel);
            Connect(parallel, "Child 1", chase);
            Connect(parallel, "Child 2", fire);

            IVariable target = graph.CreateVariable<Entity>("Target", null);
            Bind(graph, target, find.GetInputPortByName("Store in"), new Vector2(300, -170));
            Bind(graph, target, chase.GetInputPortByName("Target"), new Vector2(600, 60));
            Bind(graph, target, fire.GetInputPortByName("Target"), new Vector2(600, 440));
            IVariable radius = graph.CreateVariable("Radius", 15f);
            Bind(graph, radius, find.GetInputPortByName("Radius"), new Vector2(300, -80));
            IVariable stopDistance = graph.CreateVariable("StopDistance", 2f);
            Bind(graph, stopDistance, chase.GetInputPortByName("Stop distance"), new Vector2(600, 110));
        }

        private static T Add<T>(BrainAuthoringGraph graph, Vector2 position) where T : Node, new()
        {
            T node = new();
            graph.AddNode(node);
            node.Position = position;
            return node;
        }

        private static void Connect(Node from, string output, Node to) =>
            from.Graph.Connect(from.GetOutputPortByName(output), to.GetInputPortByName("In"));

        private static void Bind(BrainAuthoringGraph graph, IVariable variable, IPort input, Vector2 position)
        {
            IVariableNode node = graph.AddVariableNode(variable, position);
            graph.Connect(node.GetOutputPort(0), input);
        }
    }
}
#endif
