using UnityEngine;

namespace RogueLikeEngine.Surfaces
{
    /// <summary>
    /// What a surface is made of (metal, flesh, concrete...). Each game creates its own assets.
    /// </summary>
    [CreateAssetMenu(fileName = nameof(SurfaceDefinition), menuName = "Surfaces/" + nameof(SurfaceDefinition))]
    public class SurfaceDefinition : ScriptableObject
    {
    }
}
