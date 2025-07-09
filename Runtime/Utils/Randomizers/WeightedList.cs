using System;
using System.Collections.Generic;
using System.Linq;

namespace RogueLikeEngine.Utils.Randomizers
{
    [Serializable]
    public class WeightedList<T>
    {
        public List<WeightedElement<T>> Elements;

        public T GetRandomElement()
        {
            if (Elements.Count == 0) return default;
            int totalWeight = Elements.Sum(e => e.Weight);
            int randomWeight = UnityEngine.Random.Range(0, totalWeight);
            foreach (var weightedElement in Elements)
            {
                randomWeight -= weightedElement.Weight;
                if (randomWeight < 0)
                {
                    return weightedElement.element;
                }
            }
        
            return Elements.Last().element;
        }
    }
}
