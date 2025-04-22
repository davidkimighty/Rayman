namespace Rayman
{
    public interface ISpatialStructure
    {
        int Count { get; }
        int MaxHeight { get; }
        
        float CalculateCost();
#if UNITY_EDITOR
        void DrawStructure();
#endif
    }
    
    public interface ISpatialStructure<T> : ISpatialStructure where T : struct, IBounds<T>
    {
        SpatialNode<T> Root { get; }
        
        void AddLeafNode(int id, T bounds);
        void RemoveLeafNode(int id);
        void UpdateBounds(int id, T updatedBounds);
    }
}
