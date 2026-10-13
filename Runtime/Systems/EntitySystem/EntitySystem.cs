using System.Collections.Generic;
using UnityEngine;

namespace RogueLikeEngine.Systems.Entities
{
    public abstract class EntitySystem : MonoBehaviour, IEntitySystem
    {
        [SerializeField] private Entity m_entity;

        public Entity Entity => m_entity;

        public bool IsSystemActive
        {
            get => m_isSystemActive && m_locks.Count == 0;
            set
            {
                m_isSystemActive = value;
                OnSystemActiveChanged();
            }
        }

        private bool m_isSystemActive = true;
        private readonly HashSet<object> m_locks = new();

        public void Lock(object owner)
        {
            if (m_locks.Add(owner)) OnSystemActiveChanged();
        }

        public void Unlock(object owner)
        {
            if (m_locks.Remove(owner)) OnSystemActiveChanged();
        }

        protected virtual void OnSystemActiveChanged() {}
    }
}
