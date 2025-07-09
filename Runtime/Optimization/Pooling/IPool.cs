namespace RogueLikeEngine.Optimization.Pooling
{
    public interface IPool
    {
        void ReturnToPool(IPoolObject obj);
    }
}