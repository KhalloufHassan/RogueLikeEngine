namespace RogueLikeEngine.Optimization.Pooling
{
    public interface IPoolObject
    {
        IPool ParentPool { get; set; }
        bool IsDisposed { get; set; }
        void OnRequested();
        void OnDisposed();
    }
}

