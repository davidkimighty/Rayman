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
        void DrawStructure();
#endif
    }
    
    public class BoundingVolume<T> where T : struct, IBounds<T>
    {
        public RaymarchEntity Source;
        public T Bounds;

        public BoundingVolume(RaymarchEntity source)
        {
            Source = source;
            Bounds = source.GetBounds<T>();
        }
        
        public void SyncVolume(ref ISpatialStructure<T> structure)
        {
            T buffBounds = Bounds.Expand(Source.UpdateBoundsThreshold);
            T newBounds = Source.GetBounds<T>();
            if (buffBounds.Contains(newBounds)) return;

            Bounds = newBounds;
            structure.UpdateBounds(Source, newBounds);
        }
    }
}
