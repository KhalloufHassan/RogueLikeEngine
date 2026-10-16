using System.Collections.Generic;
using RogueLikeEngine.Systems.Entities;

namespace RogueLikeEngine.Systems.Brains
{
    /// <summary>The entity and variables belonging to one brain's graph instance.</summary>
    public sealed class BrainContext
    {
        private readonly Dictionary<string, BrainVariable> m_variables = new();

        public Entity Entity { get; }
        public float DeltaTime { get; internal set; }

        public BrainContext(Entity entity, IEnumerable<BrainVariable> variables)
        {
            Entity = entity;
            foreach (BrainVariable variable in variables)
                m_variables.Add(variable.Name, variable);
        }

        public T Get<T>(string name) => ((BrainVariable<T>)m_variables[name]).Value;
        public void Set<T>(string name, T value) => ((BrainVariable<T>)m_variables[name]).Value = value;

        internal void ApplyOverrides(IEnumerable<BrainVariable> overrides)
        {
            foreach (BrainVariable variable in overrides)
            {
                if (variable != null && !string.IsNullOrEmpty(variable.Name) &&
                    m_variables.TryGetValue(variable.Name, out BrainVariable target) && target.GetType() == variable.GetType())
                    variable.ApplyTo(this);
            }
        }
    }
}
