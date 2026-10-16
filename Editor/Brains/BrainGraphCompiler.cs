#if UNITY_6000_6_OR_NEWER
using System;
using System.Collections.Generic;
using System.Linq;
using RogueLikeEngine.Systems.Brains;
using RogueLikeEngine.Systems.Entities;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace RogueLikeEngine.EditorTools.Brains
{
    /// <summary>Converts authoring nodes and blackboard bindings into an editor-independent behavior tree.</summary>
    public sealed class BrainGraphCompiler
    {
        private readonly BrainAuthoringGraph m_graph;
        private readonly Dictionary<Hash128, INode> m_portOwners = new();
        private readonly HashSet<INode> m_visited = new();

        public BrainGraphCompiler(BrainAuthoringGraph graph)
        {
            m_graph = graph;
            foreach (INode node in graph.GetNodes())
            {
                foreach (IPort port in node.GetInputPorts().Concat(node.GetOutputPorts()))
                    m_portOwners.Add(port.ID, node);
            }
        }

        public void Validate()
        {
            Variables();
            Root();
        }

        public BrainNode Root()
        {
            m_visited.Clear();
            EntryNode[] entries = m_graph.GetNodes().OfType<EntryNode>().ToArray();
            if (entries.Length != 1) throw new InvalidOperationException("A brain graph needs exactly one Entry node.");
            return Child(entries[0].GetOutputPortByName("Root"));
        }

        public BrainNode[] Children(BrainGraphNode node) => node.GetOutputPorts().Select(Child).ToArray();

        private BrainNode Child(IPort output)
        {
            List<IPort> connections = new();
            output.GetConnectedPorts(connections);
            if (connections.Count != 1)
                throw new InvalidOperationException($"Connect exactly one action to '{output.DisplayName}'.");
            INode node = m_portOwners[connections[0].ID];
            if (node is not BrainGraphNode action)
                throw new InvalidOperationException("Execution ports must connect to brain actions.");
            if (!m_visited.Add(node))
                throw new InvalidOperationException("A behavior tree cannot contain cycles or share an action between branches.");
            return action.Build(this);
        }

        public BrainValue<T> Value<T>(IPort input)
        {
            if (!input.IsConnected)
            {
                if (!input.TryGetValue(out T value))
                    throw new InvalidOperationException($"'{input.DisplayName}' needs a value of type {typeof(T).Name}.");
                return new BrainValue<T> { Constant = value };
            }

            INode source = m_portOwners[input.FirstConnectedPort.ID];
            if (source is IVariableNode) return new BrainValue<T> { Variable = Variable<T>(input) };
            if (source is IConstantNode constant && constant.TryGetValue(out T constantValue))
                return new BrainValue<T> { Constant = constantValue };
            throw new InvalidOperationException($"Connect a blackboard variable or constant to '{input.DisplayName}'.");
        }

        public string Variable<T>(IPort input)
        {
            if (!input.IsConnected || m_portOwners[input.FirstConnectedPort.ID] is not IVariableNode variable)
                throw new InvalidOperationException($"'{input.DisplayName}' must be connected to a blackboard variable.");
            if (variable.Variable.DataType != typeof(T))
                throw new InvalidOperationException($"'{input.DisplayName}' requires a {typeof(T).Name} variable.");
            return variable.Variable.Name;
        }

        public BrainVariable[] Variables()
        {
            List<BrainVariable> variables = new();
            HashSet<string> names = new(StringComparer.Ordinal);
            foreach (IVariable variable in m_graph.GetVariables())
            {
                if (string.IsNullOrWhiteSpace(variable.Name) || !names.Add(variable.Name))
                    throw new InvalidOperationException("Blackboard variable names must be nonempty and unique.");
                BrainVariable value;
                if (variable.DataType == typeof(bool)) value = Default<bool, BrainBool>(variable);
                else if (variable.DataType == typeof(int)) value = Default<int, BrainInt>(variable);
                else if (variable.DataType == typeof(float)) value = Default<float, BrainFloat>(variable);
                else if (variable.DataType == typeof(string)) value = Default<string, BrainString>(variable);
                else if (variable.DataType == typeof(Vector3)) value = Default<Vector3, BrainVector3>(variable);
                else if (variable.DataType == typeof(Entity)) value = Default<Entity, BrainEntity>(variable);
                else throw new InvalidOperationException($"Unsupported brain variable type: {variable.DataType.Name}.");
                variables.Add(value);
            }
            return variables.ToArray();
        }

        private static TVariable Default<T, TVariable>(IVariable variable) where TVariable : BrainVariable<T>, new()
        {
            variable.TryGetDefaultValue(out T value);
            return new TVariable { Name = variable.Name, Value = value };
        }
    }
}
#endif
