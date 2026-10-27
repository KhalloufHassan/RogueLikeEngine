using System;
using UnityEngine;

namespace RogueLikeEngine.Cameras
{
    /// <summary>
    /// Camera requests from gameplay, so effects never reference the camera itself.
    /// A listener on the camera subscribes and applies them with whatever camera system the game uses.
    /// </summary>
    [CreateAssetMenu(fileName = nameof(CameraEventsChannel), menuName = "Cameras/" + nameof(CameraEventsChannel))]
    public class CameraEventsChannel : ScriptableObject
    {
        public event Action<Vector3> OnShake;

        /// <summary>Kicks the camera along <paramref name="impulse"/>, its length in meters.</summary>
        public void Shake(Vector3 impulse) => OnShake?.Invoke(impulse);
    }
}
