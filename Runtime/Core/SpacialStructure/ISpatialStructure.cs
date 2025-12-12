namespace Rayman
{
    public interface ISpatialStructure<T> where T : struct, IBounds<T>
    {
        SpatialNode<T> Root { get; }

        void AddLeafNode(int id, T bounds);
        void RemoveLeafNode(int id);
        void UpdateBounds(int id, T updatedBounds);
    }
}
