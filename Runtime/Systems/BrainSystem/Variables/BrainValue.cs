using System;

namespace RogueLikeEngine.Systems.Brains
{
    /// <summary>An action input bound to a graph variable or an inline constant.</summary>
    [Serializable]
    public struct BrainValue<T>
    {
        public string Variable;
        public T Constant;

        public T Get(BrainContext context) => string.IsNullOrEmpty(Variable) ? Constant : context.Get<T>(Variable);
    }
}
