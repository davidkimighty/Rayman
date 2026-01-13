using System;
using System.Collections.Generic;

namespace Rayman
{
    [Serializable]
    public struct ShapeGroup
    {
        public event Action<ShapeGroup, int> OnChange;

        public OperationType Operation;
        public float Blend;
        public List<ShapeProvider> Shapes;

        private bool isDirty;

        public ShapeProvider this[int index]
        {
            get => Shapes[index];
            set => SetShape(index, value);
        }

        public void SetShape(int index, ShapeProvider value)
        {
            Shapes[index] = value;
            isDirty = true;
            OnChange?.Invoke(this, index);
        }
    }
}