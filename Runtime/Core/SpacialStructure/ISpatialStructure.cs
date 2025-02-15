namespace Rayman
{
    public interface ISpatialStructure<T> where T : struct, IBounds<T>
    {
        SpatialNode<T> Root { get; }
        int Count { get; }
        int MaxHeight { get; }
        
        void AddLeafNode(int id, T bounds, IBoundsProvider provider);
        void RemoveLeafNode(IBoundsProvider provider);
        void UpdateBounds(IBoundsProvider provider, T updatedBounds);
        float CalculateCost();
#if UNITY_EDITOR
        void DrawStructure();
#endif
    }
}
