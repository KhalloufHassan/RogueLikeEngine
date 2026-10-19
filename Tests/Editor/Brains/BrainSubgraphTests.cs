#if UNITY_6000_6_OR_NEWER
using System;
using System.Linq;
using NUnit.Framework;
using RogueLikeEngine.EditorTools.Brains;
using RogueLikeEngine.Systems.Brains;
using RogueLikeEngine.Systems.Brains.Nodes;
using Unity.GraphToolkit.Editor;
using UnityEditor;

namespace RogueLikeEngine.Tests
{
    public class BrainSubgraphTests
    {
        private const string RootPort = "Root";
        private const string InputPort = "In";
        private const string GraphOption = "Graph";
        private const string Duration = "Duration";
        private const string TitleOption = "Title";
        private const string FirstChild = "Child 1";
        private const string SecondChild = "Child 2";
        private const string FirstName = "Child 1 name";
        private string m_folder;
        private string m_childPath;
        private BrainGraph m_asset;
        private BrainAuthoringGraph m_child;
        private BrainAuthoringGraph m_parent;

        [SetUp]
        public void SetUp()
        {
            string folder = $"BrainSubgraphTest-{Guid.NewGuid():N}";
            AssetDatabase.CreateFolder("Assets", folder);
            m_folder = "Assets/" + folder;
            m_childPath = m_folder + "/Child.rlebrain";
            m_child = GraphDatabase.CreateGraph<BrainAuthoringGraph>(m_childPath);
            EntryNode entry = Add<EntryNode>(m_child);
            WaitNode wait = Add<WaitNode>(m_child);
            m_child.Connect(entry.GetOutputPortByName(RootPort), wait.GetInputPortByName(InputPort));
            IVariable duration = m_child.CreateVariable(Duration, 2f);
            IVariableNode variable = m_child.AddVariableNode(duration, default);
            m_child.Connect(variable.GetOutputPort(0), wait.GetInputPortByName(Duration));
            GraphDatabase.SaveGraph(m_child);
            AssetDatabase.ImportAsset(m_childPath, ImportAssetOptions.ForceSynchronousImport);
            m_asset = AssetDatabase.LoadAssetAtPath<BrainGraph>(m_childPath);
            m_parent = GraphDatabase.CreateGraph<BrainAuthoringGraph>(m_folder + "/Parent.rlebrain");
            m_parent.CreateVariable(Duration, 4f);
        }

        [TearDown]
        public void TearDown() => AssetDatabase.DeleteAsset(m_folder);

        [Test]
        public void RepeatedSubgraphs_CompileIndependentNodesWithSharedBindings()
        {
            SequenceNode sequence = AddSequence();
            ConnectSubgraph(sequence, FirstChild);
            ConnectSubgraph(sequence, SecondChild);
            string dependency = null;
            BrainGraphCompiler compiler = new(m_parent, path => dependency = path);
            BrainSequence root = (BrainSequence)compiler.Root();
            Assert.AreNotSame(root.Children[0], root.Children[1]);
            Assert.AreEqual(Duration, ((Wait)root.Children[0]).Duration.Variable);
            Assert.AreEqual(Duration, ((Wait)root.Children[1]).Duration.Variable);
            Assert.AreEqual(4f, ((BrainFloat)compiler.Variables().Single()).Value);
            Assert.AreEqual(m_childPath, dependency);
        }

        [Test]
        public void Renaming_LeavesConnectionsAndExecutionOrderIntactAfterReload()
        {
            SequenceNode sequence = AddSequence();
            ConnectSubgraph(sequence, FirstChild);
            WaitNode wait = Add<WaitNode>(m_parent);
            m_parent.Connect(sequence.GetOutputPortByName(SecondChild), wait.GetInputPortByName(InputPort));
            sequence.GetNodeOptionByName(TitleOption).TrySetValue("Hide and Shoot");
            sequence.GetNodeOptionByName(FirstName).TrySetValue("Hide");
            sequence.DefineNode();
            Assert.AreEqual("Hide and Shoot", sequence.Title);
            Assert.AreEqual("1. Hide", sequence.GetOutputPortByName(FirstChild).DisplayName);
            Assert.IsTrue(sequence.GetOutputPortByName(FirstChild).IsConnected);
            GraphDatabase.SaveGraph(m_parent);
            BrainAuthoringGraph reloaded = GraphDatabase.LoadGraphForImporter<BrainAuthoringGraph>(GraphDatabase.GetGraphAssetPath(m_parent));
            SequenceNode saved = reloaded.GetNodes().OfType<SequenceNode>().Single();
            Assert.AreEqual("Hide and Shoot", saved.Title);
            Assert.AreEqual("1. Hide", saved.GetOutputPortByName(FirstChild).DisplayName);
            BrainSequence root = (BrainSequence)new BrainGraphCompiler(reloaded).Root();
            Assert.AreEqual(Duration, ((Wait)root.Children[0]).Duration.Variable);
            Assert.IsTrue(string.IsNullOrEmpty(((Wait)root.Children[1]).Duration.Variable));
        }

        [Test]
        public void MissingOrMismatchedSharedVariable_IsRejected()
        {
            EntryNode entry = Add<EntryNode>(m_parent);
            ConnectSubgraph(entry, RootPort);
            m_parent.RemoveVariable(m_parent.GetVariable(0));
            Assert.Throws<InvalidOperationException>(() => new BrainGraphCompiler(m_parent).Root());
            m_parent.CreateVariable(Duration, false);
            Assert.Throws<InvalidOperationException>(() => new BrainGraphCompiler(m_parent).Root());
        }

        [Test]
        public void SelfReference_IsRejected()
        {
            foreach (INode node in m_child.GetNodes().ToArray()) m_child.RemoveNode(node);
            EntryNode entry = Add<EntryNode>(m_child);
            SubgraphNode subgraph = Add<SubgraphNode>(m_child);
            subgraph.GetNodeOptionByName(GraphOption).TrySetValue(m_asset);
            m_child.Connect(entry.GetOutputPortByName(RootPort), subgraph.GetInputPortByName(InputPort));
            Assert.Throws<InvalidOperationException>(() => new BrainGraphCompiler(m_child).Root());
        }

        [Test]
        public void MissingSubgraph_IsRejected()
        {
            EntryNode entry = Add<EntryNode>(m_parent);
            SubgraphNode subgraph = Add<SubgraphNode>(m_parent);
            m_parent.Connect(entry.GetOutputPortByName(RootPort), subgraph.GetInputPortByName(InputPort));
            Assert.Throws<InvalidOperationException>(() => new BrainGraphCompiler(m_parent).Root());
        }

        private SequenceNode AddSequence()
        {
            EntryNode entry = Add<EntryNode>(m_parent);
            SequenceNode sequence = Add<SequenceNode>(m_parent);
            m_parent.Connect(entry.GetOutputPortByName(RootPort), sequence.GetInputPortByName(InputPort));
            return sequence;
        }

        private void ConnectSubgraph(Node parent, string port)
        {
            SubgraphNode subgraph = Add<SubgraphNode>(m_parent);
            subgraph.GetNodeOptionByName(GraphOption).TrySetValue(m_asset);
            m_parent.Connect(parent.GetOutputPortByName(port), subgraph.GetInputPortByName(InputPort));
        }

        private static T Add<T>(BrainAuthoringGraph graph) where T : Node, new()
        {
            T node = new();
            graph.AddNode(node);
            return node;
        }
    }
}
#endif
