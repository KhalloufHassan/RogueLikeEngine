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
    public class BrainGraphTests
    {
        private string m_path;
        private BrainAuthoringGraph m_graph;

        [SetUp]
        public void SetUp()
        {
            m_path = $"Assets/BrainGraphTest-{Guid.NewGuid():N}.rlebrain";
            m_graph = GraphDatabase.CreateGraph<BrainAuthoringGraph>(m_path);
            BrainGraphCreation.Populate(m_graph);
        }

        [TearDown]
        public void TearDown() => AssetDatabase.DeleteAsset(m_path);

        [Test]
        public void Template_ImportsAndReloadsAsAnAssignableRuntimeAsset()
        {
            GraphDatabase.SaveGraph(m_graph);
            AssetDatabase.ImportAsset(m_path, ImportAssetOptions.ForceSynchronousImport);
            BrainGraph asset = AssetDatabase.LoadAssetAtPath<BrainGraph>(m_path);
            Assert.IsNotNull(asset);
            BrainSequence root = asset.Root as BrainSequence;
            Assert.IsNotNull(root);
            Assert.IsInstanceOf<FindTarget>(root.Children[0]);
            BrainParallel parallel = root.Children[1] as BrainParallel;
            Assert.IsNotNull(parallel);
            Assert.IsInstanceOf<ChaseTarget>(parallel.Children[0]);
            Assert.IsInstanceOf<FireAtTarget>(parallel.Children[1]);
            Assert.AreEqual("Target", ((FindTarget)root.Children[0]).TargetVariable);
            Assert.AreEqual("Target", ((ChaseTarget)parallel.Children[0]).Target.Variable);
            Assert.AreEqual("Radius", ((FindTarget)root.Children[0]).Radius.Variable);
            Assert.AreEqual("StopDistance", ((ChaseTarget)parallel.Children[0]).StopDistance.Variable);
            Assert.IsInstanceOf<BrainEntity>(asset.Variables.Single(variable => variable.Name == "Target"));
            Assert.AreEqual(15f, ((BrainFloat)asset.Variables.Single(variable => variable.Name == "Radius")).Value);
            Assert.AreEqual(2f, ((BrainFloat)asset.Variables.Single(variable => variable.Name == "StopDistance")).Value);

            BrainAuthoringGraph reloaded = GraphDatabase.LoadGraphForImporter<BrainAuthoringGraph>(m_path);
            Assert.DoesNotThrow(() => new BrainGraphCompiler(reloaded).Validate());
        }

        [Test]
        public void VariableRename_IsReflectedInActionBindings()
        {
            m_graph.GetVariables().Single(variable => variable.Name == "Target").Name = "Enemy";
            BrainGraphCompiler compiler = new(m_graph);
            BrainSequence root = (BrainSequence)compiler.Root();
            Assert.AreEqual("Enemy", ((FindTarget)root.Children[0]).TargetVariable);
            Assert.AreEqual("Enemy", compiler.Variables()[0].Name);
        }

        [Test]
        public void MissingChild_IsRejected()
        {
            FireAtTargetNode fire = m_graph.GetNodes().OfType<FireAtTargetNode>().Single();
            fire.RemoveFromGraph();
            Assert.Throws<InvalidOperationException>(() => new BrainGraphCompiler(m_graph).Validate());
        }

        [Test]
        public void MissingVariableBinding_IsRejected()
        {
            FindTargetNode find = m_graph.GetNodes().OfType<FindTargetNode>().Single();
            IPort input = find.GetInputPortByName("Store in");
            m_graph.Disconnect(input.FirstConnectedPort, input);
            Assert.Throws<InvalidOperationException>(() => new BrainGraphCompiler(m_graph).Validate());
        }

        [Test]
        public void MultipleEntries_AreRejected()
        {
            m_graph.AddNode(new EntryNode());
            Assert.Throws<InvalidOperationException>(() => new BrainGraphCompiler(m_graph).Validate());
        }
    }
}
#endif
