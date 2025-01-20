using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Rayman
{
    [CreateAssetMenu(menuName = "Rayman/Data Provider/BVH AABB Node Data")]
    public class BvhAabbNodeDataProvider : NodeDataProvider<Aabb, NodeDataAABB>
    {
        public override string GetDebugMessage()
        {
            int sum = nodeDataByGroup.Sum(g => g.Value.Structure.Count);
            int maxHeight = nodeDataByGroup.Max(g => g.Value.Structure.MaxHeight);
            return $"BVH {nodeDataByGroup.Count} [ Nodes {sum,4}, Max Height {maxHeight,2} ]";
        }
        
        protected override ISpatialStructure<Aabb> CreateSpatialStructure(BoundingVolume<Aabb>[] volumes)
        {
            return Bvh<Aabb>.Create(volumes);
        }

        protected override int GetNodeStride()
        {
            return NodeDataAABB.Stride;
        }

        protected override void UpdateNodeData(ISpatialStructure<Aabb> structure, ref NodeDataAABB[] nodeData)
        {
            int index = 0;
            Queue<(SpatialNode<Aabb> node, int parentIndex)> queue = new();
            queue.Enqueue((structure.Root, -1));

            while (queue.Count > 0)
            {
                (SpatialNode<Aabb> current, int parentIndex) = queue.Dequeue();
                NodeDataAABB data = new()
                {
                    Id = current.Id,
                    ChildIndex = -1,
                    Bounds = current.Bounds,
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
    
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct NodeDataAABB
    {
        public const int Stride = sizeof(float) * 6 + sizeof(int) * 2;

        public int Id;
        public int ChildIndex;
        public Aabb Bounds;
    }
}
