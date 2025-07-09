using System;

namespace RogueLikeEngine.Utils.Randomizers
{
    [Serializable]
    public class ChanceElement<T> : Chance
    {
        public T element;
    }
}
