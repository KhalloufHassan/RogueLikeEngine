namespace RogueLikeEngine.Systems.Entities
{
    public interface IOnHit
    {
        void OnHit(Entity entity, HitInfo hit);
    }
}