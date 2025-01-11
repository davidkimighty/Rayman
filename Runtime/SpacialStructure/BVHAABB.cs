using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Rayman
{
    [ExecuteInEditMode]
    public class BVHAABB : SpatialStructure
    {
#if UNITY_EDITOR
        [Header("Debugging")]
        [SerializeField] protected bool executeInEditor;
        [SerializeField] protected bool drawGizmos;
#endif
        private ISpatialStructure<AABB> bvh;
        private BoundingVolume<AABB>[] volumes;
        private NodeDataAABB[] nodeData;
        
        public bool IsInitialized => bvh != null;

        private void Update()
        {
            if (volumes == null) return;
            
            for (int i = 0; i < volumes.Length; i++)
                volumes[i].SyncVolume(ref bvh);
        }

        public override void Setup(List<RaymarchEntity> entities)
        {
            Build(entities);
        }

        public override void SetData(GraphicsBuffer nodeBuffer)
        {
            if (nodeData == null || nodeData.Length == 0) return;

            UpdateNodeData();
            nodeBuffer.SetData(nodeData);
        }

        public void Build(List<RaymarchEntity> entities)
        {
            if (entities.Count == 0) return;
            
            volumes = entities.Select(e => new BoundingVolume<AABB>(e)).ToArray();
            bvh = BVH<AABB>.Create(volumes);
        }
        
        protected void UpdateNodeData()
        {
            int index = 0;
            Queue<(SpatialNode<AABB> node, int parentIndex)> queue = new();
            queue.Enqueue((bvh.Root, -1));

            while (queue.Count > 0)
            {
                (SpatialNode<AABB> current, int parentIndex) = queue.Dequeue();
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
        
#if UNITY_EDITOR
        protected virtual void OnDrawGizmos()
        {
            if (!drawGizmos) return;
            
            if (!executeInEditor)
            {
                Gizmos.color = new Color(1, 1, 1, 0.3f);
                Gizmos.DrawSphere(transform.position, 0.1f);
            }

            if (IsInitialized)
                bvh?.DrawStructure();
        }
#endif
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct NodeDataAABB
    {
        public const int Stride = sizeof(float) * 6 + sizeof(int) * 2;

        public int Id;
        public int ChildIndex;
        public AABB Bounds;
    }
}
