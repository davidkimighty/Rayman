using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Rayman
{
    public class BVHAABB : SpatialStructure
    {
        [StructLayout(LayoutKind.Sequential, Pack = 0)]
        public struct NodeData
        {
            public const int Stride = sizeof(float) * 6 + sizeof(int) * 2;

            public int Id;
            public int ChildIndex;
            public AABB Bounds;
        }
        
        private static int NodeBufferId = Shader.PropertyToID("_NodeBuffer");

#if UNITY_EDITOR
        [Header("Debugging")]
        [SerializeField] protected bool executeInEditor;
        [SerializeField] protected bool drawGizmos;
        [SerializeField] protected int boundsDisplayThreshold = 300;
#endif
        
        private ISpatialStructure<AABB> bvh;
        private BoundingVolume<AABB>[] volumes;
        private NodeData[] nodeData;

        public bool IsInitialized => bvh != null && nodeData != null;
        
        public override void Setup(ref Material mat, RaymarchEntity[] entities)
        {
            if (entities.Length == 0) return;
            
            volumes = entities.Select(e => new BoundingVolume<AABB>(e)).ToArray();
            bvh = BVH<AABB>.Create(volumes);
            if (bvh == null) return;

            int nodesCount = SpatialNode<AABB>.GetNodesCount(bvh.Root);
            nodeData = new NodeData[nodesCount];
            
            NodeBuffer?.Release();
            NodeBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, nodesCount, NodeData.Stride);
            mat.SetBuffer(NodeBufferId, NodeBuffer);
        }

        public override void SetData()
        {
            if (!IsInitialized) return;
            
            for (int i = 0; i < volumes.Length; i++)
                volumes[i].SyncVolume(ref bvh);

            UpdateNodeData();
            NodeBuffer.SetData(nodeData);
        }

        public override void Release()
        {
            NodeBuffer?.Release();
            bvh = null;
            volumes = null;
            nodeData = null;
        }

        private void UpdateNodeData()
        {
            int index = 0;
            Queue<(SpatialNode<AABB> node, int parentIndex)> queue = new();
            queue.Enqueue((bvh.Root, -1));

            while (queue.Count > 0)
            {
                (SpatialNode<AABB> current, int parentIndex) = queue.Dequeue();
                NodeData data = new()
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
        
#if UNITY_EDITOR
        protected virtual void OnDrawGizmos()
        {
            if (!drawGizmos) return;
            
            if (IsInitialized)
                bvh?.DrawStructure();
        }
#endif
    }
}
