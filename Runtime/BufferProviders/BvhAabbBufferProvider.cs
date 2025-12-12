using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Rayman
{
    public class BvhAabbBufferProvider : BufferProvider<IBoundsProvider>
    {
        public static readonly int BufferId = Shader.PropertyToID("_NodeBuffer");

        [SerializeField] private float syncMargin = 0.01f;
#if UNITY_EDITOR
        [SerializeField] private bool drawGizmos = false;
#endif
        private IBoundsProvider[] providers;
        private Bvh<Aabb> bvh;
        private Aabb[] bounds;
        private AabbNodeData[] nodeData;

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!IsInitialized || !drawGizmos) return;

            bvh.DrawStructure();
        }
#endif

        public override void InitializeBuffer(ref Material material, IBoundsProvider[] dataProviders)
        {
            if (dataProviders == null || dataProviders.Length == 0) return;

            if (IsInitialized)
                ReleaseBuffer();
            providers = dataProviders;

            bvh = new Bvh<Aabb>(providers);
            int count = bvh.Count;

            Buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, count, Marshal.SizeOf<AabbNodeData>());
            material.SetBuffer(BufferId, Buffer);

            bounds = new Aabb[count];
            nodeData = new AabbNodeData[count];

            for (int i = 0; i < providers.Length; i++)
                bounds[i] = providers[i].GetBounds<Aabb>();
            UpdateNodeData();
            Buffer.SetData(nodeData);
        }

        public override void SetData()
        {
            if (!IsInitialized) return;

            bool setData = false;
            for (int i = 0; i < providers.Length; i++)
            {
                IBoundsProvider provider = providers[i];
                if (provider == null) continue;

                Aabb current = provider.GetBounds<Aabb>();
                Aabb expanded = bounds[i].Expand(syncMargin);
                if (expanded.Contains(current)) continue;

                bounds[i] = current;
                bvh.UpdateBounds(i, current);
                setData = true;
            }

            if (setData)
            {
                UpdateNodeData();
                Buffer.SetData(nodeData);
            }
        }

        public override void ReleaseBuffer()
        {
            Buffer?.Release();
            Buffer = null;
            providers = null;
            bvh = null;
            bounds = null;
            nodeData = null;
        }

#if UNITY_EDITOR
        public void DrawGizmos()
        {
            if (!IsInitialized) return;

            bvh.DrawStructure();
        }
#endif

        private void UpdateNodeData()
        {
            int index = 0;
            Queue<(SpatialNode<Aabb> node, int parentIndex)> queue = new();
            queue.Enqueue((bvh.Root, -1));

            while (queue.Count > 0)
            {
                (SpatialNode<Aabb> current, int parentIndex) = queue.Dequeue();
                AabbNodeData data = new(current, -1);

                if (current.LeftChild != null)
                {
                    data.ChildIndex = index + queue.Count + 1; // setting child index
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
