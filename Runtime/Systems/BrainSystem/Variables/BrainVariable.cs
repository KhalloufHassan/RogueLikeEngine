using System;

namespace RogueLikeEngine.Systems.Brains
{
    [Serializable]
    public abstract class BrainVariable
    {
        public string Name;

        internal abstract void ApplyTo(BrainContext context);
    }
}
