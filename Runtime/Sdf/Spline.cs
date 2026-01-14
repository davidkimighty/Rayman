using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rayman
{
    [Serializable]
    public struct Spline
    {
        public event Action<Spline, int> OnChange;
        
        [HideInInspector] public int KnotStartIndex;

        [SerializeField] private float extendedBounds;
        [SerializeField] private List<KnotProvider> knots;

        private bool isDirty;
        
        public List<KnotProvider> Knots => knots;
        public float ExtendedBounds => extendedBounds;
        
        public KnotProvider this[int index]
        {
            get => knots[index];
            set => SetKnot(index, value);
        }

        public void SetKnot(int index, KnotProvider value)
        {
            knots[index] = value;
            isDirty = true;
            OnChange?.Invoke(this, index);
        }
    }
}