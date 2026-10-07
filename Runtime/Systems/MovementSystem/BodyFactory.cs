using UnityEngine;

namespace RogueLikeEngine.Systems.Movements
{
    /// <summary>Creates the body matching whichever Rigidbody an object has.</summary>
    public static class BodyFactory
    {
        public static IBody Create(Rigidbody2D rigidbody2D, Rigidbody rigidbody3D)
        {
            if (rigidbody3D) return new Body3D(rigidbody3D);
            if (rigidbody2D) return new Body2D(rigidbody2D);
            return null;
        }

        public static IBody Find(GameObject gameObject) =>
            Create(gameObject.GetComponent<Rigidbody2D>(), gameObject.GetComponent<Rigidbody>());

    }
}
