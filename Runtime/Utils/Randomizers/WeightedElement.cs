using System;
using UnityEngine;

namespace RogueLikeEngine.Utils.Randomizers
{
    [Serializable]
    public class WeightedElement<T>
    {
        public int weight;
        public T element;
    
        /// <summary>
        /// This filed is just for the inspector to show the chance on each individual element 
        /// </summary>
        [SerializeField,HideInInspector]
        private int totalWeight;
    
        public int Weight => weight;
    }
}
