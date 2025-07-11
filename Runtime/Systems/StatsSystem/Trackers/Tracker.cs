using System;
using UnityEngine;

namespace RogueLikeEngine.Systems.Stats
{
    public abstract class Tracker<T> : ScriptableObject
    {
        [SerializeField] protected T value;

        public event Action<T> OnValueChanged;

        public T Value
        {
            get => value;
            set
            {
                if (!Equals(this.value, value))
                {
                    this.value = value;
                    OnValueChanged?.Invoke(this.value);
                }
            }
        }
    }

}