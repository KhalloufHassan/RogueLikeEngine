using UnityEngine;

namespace RogueLikeEngine.Systems.Entities
{
    public interface IOnHit
    {
        void OnHit(Entity entity,Collision2D collision);
    }
}