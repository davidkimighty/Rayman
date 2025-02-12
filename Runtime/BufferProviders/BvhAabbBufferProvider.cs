using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Rayman
{
    public class BvhAabbBufferProvider : NodeBufferProvider<Aabb, NodeDataAABB>, ISpatialStructureDebugProvider
    {
        protected override ISpatialStructure<Aabb> CreateSpatialStructure(BoundingVolume<Aabb>[] volumes)
        {
            return Bvh<Aabb>.Create(volumes);
        }

        protected override int GetNodeStride()
        {
            return NodeDataAABB.Stride;
        }

        protected override void UpdateNodeData(ISpatialStructure<Aabb> structure, ref NodeDataAABB[] data)
        {
            int index = 0;
            Queue<(SpatialNode<Aabb> node, int parentIndex)> queue = new();
            queue.Enqueue((structure.Root, -1));

            while (queue.Count > 0)
            {
                (SpatialNode<Aabb> current, int parentIndex) = queue.Dequeue();
                NodeDataAABB node = new()
                {
                    Id = current.Id,
                    ChildIndex = -1,
                    Bounds = current.Bounds,
                };

                if (current.LeftChild != null)
                {
                    node.ChildIndex = index + queue.Count + 1;
                    queue.Enqueue((current.LeftChild, index));
                }
                if (current.RightChild != null)
                    queue.Enqueue((current.RightChild, index));
                
                nodeData[index] = node;
                index++;
            }
        }

        public (int nodeCount, int maxHeight) GetDebugInfo()
        {
            return (spatialStructure.Count, spatialStructure.MaxHeight);
        }
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct NodeDataAABB
    {
        public const int Stride = sizeof(float) * 6 + sizeof(int) * 2;

        public int Id;
        public int ChildIndex;
        public Aabb Bounds;
    }
}
