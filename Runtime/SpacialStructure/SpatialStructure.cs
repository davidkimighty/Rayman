using UnityEngine;

namespace Rayman
{
    public abstract class SpatialStructure<T> : MonoBehaviour, ISpatialStructure<T> where T : struct, IBounds<T>
    {
        public SpatialNode<T> Root { get; protected set; }
        public int Count { get; protected set; }
        public int MaxHeight { get; protected set; }

        public abstract void AddLeafNode(int id, T bounds, IBoundsSource source);

        public abstract void RemoveLeafNode(IBoundsSource source);

        public abstract void UpdateBounds(IBoundsSource source, T updatedBounds);

        public abstract float CalculateCost();

#if UNITY_EDITOR
        public abstract void DrawStructure();
#endif
    }
}
