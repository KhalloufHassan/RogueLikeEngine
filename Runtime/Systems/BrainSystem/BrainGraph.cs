using UnityEngine;

namespace RogueLikeEngine.Systems.Brains
{
    /// <summary>Imported behavior definition. Each brain clones it before execution.</summary>
    public sealed class BrainGraph : ScriptableObject
    {
        [SerializeReference] private BrainNode m_root;
        [SerializeReference] private BrainVariable[] m_variables = System.Array.Empty<BrainVariable>();

        public BrainNode Root => m_root;
        public BrainVariable[] Variables => m_variables;

        public void SetDefinition(BrainNode root, BrainVariable[] variables)
        {
            m_root = root;
            m_variables = variables;
        }
    }
}
