using System.Collections.Generic;
using System.Runtime.InteropServices;
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
    public struct AabbNodeData
    {
        public static readonly int Stride = sizeof(float) * 6 + sizeof(int) * 2;
        
        public int Id;
        public AABB Bounds;
        public int ChildIndex;
        
        public static void UpdateAabbNodeData(ISpatialStructure<AABB> spatialStructure, ref AabbNodeData[] nodeData)
        {
            int index = 0;
            Queue<(SpatialNode<AABB> node, int parentIndex)> queue = new();
            queue.Enqueue((spatialStructure.Root, -1));

            while (queue.Count > 0)
            {
                (SpatialNode<AABB> current, int parentIndex) = queue.Dequeue();
                AabbNodeData data = new()
                {
                    Id = current.Id,
                    Bounds = current.Bounds,
                    ChildIndex = -1
                };

                if (current.LeftChild != null)
                {
                    data.ChildIndex = index + queue.Count + 1;
                    queue.Enqueue((current.LeftChild, index));
                }
                if (current.RightChild != null)
                    queue.Enqueue((current.RightChild, index));
                
                nodeData[index] = data;
                index++;
            }
        }
    }
}
