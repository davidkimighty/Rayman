namespace Rayman
{
    public class BoundingVolume<T> where T : struct, IBounds<T>
    {
        public IBoundsProvider Source;
        public T Bounds;

        public BoundingVolume(IBoundsProvider source)
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
