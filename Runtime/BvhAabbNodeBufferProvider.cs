using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Rayman
{
    public class BvhAabbNodeBufferProvider : IBufferProvider
    {
        protected static readonly int NodeBufferId = Shader.PropertyToID("_NodeBuffer");

        protected float updateBoundsThreshold;
        protected ISpatialStructure<Aabb> spatialStructure;
        protected BoundingVolume<Aabb>[] boundingVolumes;
        
        protected NodeDataAabb[] nodeData;
        protected GraphicsBuffer nodeBuffer;

        public BvhAabbNodeBufferProvider(float updateBoundsThreshold)
        {
            this.updateBoundsThreshold = updateBoundsThreshold;
        }

        public bool IsInitialized => spatialStructure != null && boundingVolumes != null && nodeData != null;

        public void SetupBuffer(RaymarchEntity[] entities, ref Material mat)
        {
            boundingVolumes = entities.Select(e => new BoundingVolume<Aabb>(e)).ToArray();
            spatialStructure = Bvh<Aabb>.Create(boundingVolumes);
            int nodeCount = spatialStructure.Count;
            if (nodeCount == 0) return;
            
            nodeBuffer?.Release();
            nodeData = new NodeDataAabb[nodeCount];
            nodeBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, nodeCount, NodeDataAabb.Stride);
            mat.SetBuffer(NodeBufferId, nodeBuffer);
        }
        
        public void UpdateBufferData()
        {
            if (!IsInitialized) return;

            for (int i = 0; i < boundingVolumes.Length; i++)
                boundingVolumes[i].SyncVolume(ref spatialStructure, updateBoundsThreshold);

            UpdateNodeData();
            nodeBuffer.SetData(nodeData);
        }

        public void ReleaseBuffer()
        {
            nodeBuffer?.Release();
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
