using UnityEngine;

namespace RogueLikeEngine.Systems.Movements
{
    public class Cover : MonoBehaviour
    {
        [SerializeField] private CoverType m_type;

        public CoverType Type => m_type;
        public bool IsAvailable { get; internal set; }

        private void OnDisable() => IsAvailable = false;
    }
}
