using UnityEngine;

namespace RogueLikeEngine.Utils
{
    internal static class UnityObjectUtility
    {
        /// <summary>Destroy in play mode, DestroyImmediate in edit mode where Destroy isn't allowed.</summary>
        public static void DestroySafely(Object obj)
        {
            if (!obj) return;
            if (Application.isPlaying)
                Object.Destroy(obj);
            else
                Object.DestroyImmediate(obj);
        }
    }
}
