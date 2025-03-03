using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Rayman
{
    public class BvhAabbNodeBufferProvider : IBufferProvider
    {
        public static readonly int NodeBufferId = Shader.PropertyToID("_NodeBuffer");

        private float updateBoundsThreshold;
        private ISpatialStructure<Aabb> spatialStructure;
        private BoundingVolume<Aabb>[] boundingVolumes;
        private NodeDataAabb[] nodeData;

        public ISpatialStructure<Aabb> SpatialStructure => spatialStructure;

        public BvhAabbNodeBufferProvider(float updateBoundsThreshold)
        {
            this.updateBoundsThreshold = updateBoundsThreshold;
        }

        public bool IsInitialized => spatialStructure != null && boundingVolumes != null && nodeData != null;

        public GraphicsBuffer InitializeBuffer(RaymarchElement[] entities, ref Material material)
        {
            boundingVolumes = entities.Select(e => new BoundingVolume<Aabb>(e)).ToArray();
            spatialStructure = Bvh<Aabb>.Create(boundingVolumes);
            int nodeCount = spatialStructure.Count;
            if (nodeCount == 0) return null;
            
            nodeData = new NodeDataAabb[nodeCount];
            GraphicsBuffer nodeBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, nodeCount, NodeDataAabb.Stride);
            material.SetBuffer(NodeBufferId, nodeBuffer);
            return nodeBuffer;
        }
        
        public void SetData(ref GraphicsBuffer buffer)
        {
            if (!IsInitialized) return;

            for (int i = 0; i < boundingVolumes.Length; i++)
                boundingVolumes[i].SyncVolume(ref spatialStructure, updateBoundsThreshold);

            UpdateNodeData();
            buffer.SetData(nodeData);
        }

        public void ReleaseData()
        {
            spatialStructure = null;
            boundingVolumes = null;
            nodeData = null;
        }
        
#if UNITY_EDITOR
        public void DrawGizmos()
        {
            spatialStructure?.DrawStructure();
        }
#endif
        
        private void UpdateNodeData()
        {
            int index = 0;
            Queue<(SpatialNode<Aabb> node, int parentIndex)> queue = new();
            queue.Enqueue((spatialStructure.Root, -1));

            while (queue.Count > 0)
            {
                (SpatialNode<Aabb> current, int parentIndex) = queue.Dequeue();
                NodeDataAabb node = new()
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
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct NodeDataAabb
    {
        public const int Stride = sizeof(float) * 6 + sizeof(int) * 2;

        public int Id;
        public int ChildIndex;
        public Aabb Bounds;
    }
}
