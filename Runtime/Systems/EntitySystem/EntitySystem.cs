using UnityEngine;

namespace RogueLikeEngine.Systems.Entities
{
    public abstract class EntitySystem : MonoBehaviour
    {
        [SerializeField] private Entity m_entity;

        public Entity Entity => m_entity;

        public bool IsSystemActive
        {
            get => m_isSystemActive;
            set
            {
                m_isSystemActive = value;
                OnSystemActiveChanged();
            }
        }

        private bool m_isSystemActive = true;
    
        protected virtual void OnSystemActiveChanged() {}
    }
}