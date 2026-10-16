using System;

namespace RogueLikeEngine.Systems.Brains
{
    [Serializable]
    public abstract class BrainVariable<T> : BrainVariable
    {
        public T Value;

        internal override void ApplyTo(BrainContext context) => context.Set(Name, Value);
    }
}
