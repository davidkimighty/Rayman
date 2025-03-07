using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Rayman
{
    public class BvhAabbNodeBufferProvider
    {
        public static readonly int NodeBufferId = Shader.PropertyToID("_NodeBuffer");

        private ISpatialStructure<Aabb> spatialStructure;
        private Aabb[] activeBounds;
        private AabbNodeData[] nodeData;

        public ISpatialStructure<Aabb> SpatialStructure => spatialStructure;

        public bool IsInitialized => spatialStructure != null && nodeData != null;
        
        public GraphicsBuffer InitializeBuffer(ref Material material, Aabb[] bounds)
        {
            activeBounds = bounds;
            spatialStructure = new Bvh<Aabb>();
            
            for (int i = 0; i < bounds.Length; i++)
                spatialStructure.AddLeafNode(i, bounds[i]);
            
            int count = spatialStructure.Count;
            if (count == 0) return null;
            
            nodeData = new AabbNodeData[count];
            GraphicsBuffer nodeBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, count, AabbNodeData.Stride);
            material.SetBuffer(NodeBufferId, nodeBuffer);
            return nodeBuffer;
        }

        public void SyncBounds(int id, Aabb bounds, float threshold = 0f)
        {
            Aabb buffBounds = activeBounds[id].Expand(threshold);
            if (buffBounds.Contains(bounds)) return;

            activeBounds[id] = bounds;
            spatialStructure.UpdateBounds(id, bounds);
        }
        
        public void SetData(ref GraphicsBuffer buffer)
        {
            if (!IsInitialized) return;

            UpdateNodeData();
            buffer.SetData(nodeData);
        }

        public void ReleaseData()
        {
            spatialStructure = null;
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
                AabbNodeData node = new()
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
    public struct AabbNodeData
    {
        public const int Stride = sizeof(float) * 6 + sizeof(int) * 2;

        public int Id;
        public int ChildIndex;
        public Aabb Bounds;
    }
}
