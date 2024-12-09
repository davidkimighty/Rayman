using System;
using System.Threading.Tasks;

namespace Rayman
{
    [Serializable]
    public struct BoundingVolume<T> where T : struct, IBounds<T>
    {
        public IBoundsSource Source;
        public T Bounds;
    }
    
    public interface ISpatialStructure<T> where T : struct, IBounds<T>
    {
        SpatialNode<T> Root { get; }
        int Count { get; }

        void Build(BoundingVolume<T>[] boundsToBuild);
        Task BuildAsync(BoundingVolume<T>[] boundsToBuild);
        void UpdateBounds(int index, T updatedBounds);
        float CalculateCost();
#if UNITY_EDITOR
        void DrawStructure();
#endif
    }
}
