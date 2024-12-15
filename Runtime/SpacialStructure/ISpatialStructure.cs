namespace Rayman
{
    public interface ISpatialStructure<T> where T : struct, IBounds<T>
    {
        SpatialNode<T> Root { get; }
        int Count { get; }
        int MaxHeight { get; }
        
        void AddLeafNode(int id, T bounds, IBoundsSource source);
        void RemoveLeafNode(IBoundsSource source);
        void UpdateBounds(IBoundsSource source, T updatedBounds);
        float CalculateCost();
#if UNITY_EDITOR
        void DrawStructure(bool showLabel);
#endif
    }
}
