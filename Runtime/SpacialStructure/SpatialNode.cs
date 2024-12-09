using UnityEngine;

namespace Rayman
{
    public class SpatialNode<T> where T : struct, IBounds<T>
    {
        public int Id;
        public T Bounds;
        public IBoundsSource Source;
        public SpatialNode<T> Parent;
        public SpatialNode<T> LeftChild;
        public SpatialNode<T> RightChild;
        public int Height;

        public bool IsLeaf => Id != -1;

        public SpatialNode() { }
        
        public SpatialNode(int id, T bounds, int height)
        {
            Id = id;
            Bounds = bounds;
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
