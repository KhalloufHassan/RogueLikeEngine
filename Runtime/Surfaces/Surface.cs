using UnityEngine;

namespace RogueLikeEngine.Surfaces
{
    /// <summary>
    /// Marks what this object and its children are made of. Objects without one count as the default surface.
    /// </summary>
    public class Surface : MonoBehaviour
    {
        [SerializeField] private SurfaceDefinition m_definition;

        /// <summary>Can change at runtime, for example while a shield is up.</summary>
        public SurfaceDefinition Definition
        {
            get => m_definition;
            set => m_definition = value;
        }

        /// <summary>The surface of the collider's object or its closest parent, null for the default surface.</summary>
        public static SurfaceDefinition Of(Component collider)
        {
            Surface surface = collider ? collider.GetComponentInParent<Surface>() : null;
            return surface ? surface.Definition : null;
        }
    }
}
