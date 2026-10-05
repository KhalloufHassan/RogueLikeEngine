namespace RogueLikeEngine.Systems.Entities
{
    public interface IEntitySystem
    {
        Entity Entity { get; }
        bool IsSystemActive { get; set; }
    }
}
