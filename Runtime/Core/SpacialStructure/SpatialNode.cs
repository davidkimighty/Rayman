using System.Runtime.InteropServices;
using UnityEngine;

namespace Rayman
{
    public class SpatialNode<T> where T : struct, IBounds<T>
    {
        public const int InternalNodeId = -1;
        
        public int Id;
        public int Height;
        public SpatialNode<T> Parent;
        public SpatialNode<T> LeftChild;
        public SpatialNode<T> RightChild;
        public T Bounds;

        public bool IsLeaf => Id != InternalNodeId;
        
        public SpatialNode() { }
        
        public SpatialNode(int id, int height, T bounds)
        {
            Id = id;
            Height = height;
            Bounds = bounds;
        }
        
        public static int GetNodesCount(SpatialNode<T> node)
        {
            if (node == null) return 0;
            
            int count = 1;
            if (node.LeftChild != null)
                count += GetNodesCount(node.LeftChild);
            if (node.RightChild != null)
                count += GetNodesCount(node.RightChild);
            return count;
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

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct AabbNodeData : ISetupFromIndexed<SpatialNode<Aabb>>
    {
        public const int Stride = sizeof(float) * 6 + sizeof(int) * 2;

        public int Id;
        public int ChildIndex;
        public Aabb Bounds;
        
        public int Index
        {
            get => ChildIndex;
            set => ChildIndex = value;
        }
        
        public void SetupFrom(SpatialNode<Aabb> data, int index)
        {
            Id = data.Id;
            ChildIndex = index;
            Bounds = data.Bounds;
        }
    }
}
