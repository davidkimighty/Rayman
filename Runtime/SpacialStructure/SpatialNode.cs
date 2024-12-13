using UnityEngine;

namespace Rayman
{
    public class SpatialNode<T> where T : struct, IBounds<T>
    {
        public const int InternalNodeId = -1;
        
        public int Id;
        public T Bounds;
        public IBoundsSource Source;
        public int Height;
        public SpatialNode<T> Parent;
        public SpatialNode<T> LeftChild;
        public SpatialNode<T> RightChild;

        public bool IsLeaf => Id != InternalNodeId;
        
        public SpatialNode() { }
        
        public SpatialNode(int id, T bounds, IBoundsSource source, int height)
        {
            Id = id;
            Bounds = bounds;
            Source = source;
            Height = height;
        }
        
        public void UpdateHeight()
        {
            int heightL = LeftChild?.Height ?? 0;
            int heightR = RightChild?.Height ?? 0;
            Height = 1 + Mathf.Max(heightL, heightR);
        }

        public void UpdateBounds()
        {
            Bounds = LeftChild.Bounds.Union(RightChild.Bounds);
        }

        public int GetBalanceFactor()
        {
            int heightL = LeftChild?.Height ?? 0;
            int heightR = RightChild?.Height ?? 0;
            return heightR - heightL;
        }
    }
}
