using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Rayman
{
    public class BvhAabbBufferProvider : BufferProvider<IBoundsProvider>
    {
        public static readonly int BufferId = Shader.PropertyToID("_NodeBuffer");
        public static readonly int Stride = UnsafeUtility.SizeOf<AabbNodeData>();

        [SerializeField] private float syncMargin = 0.01f;
#if UNITY_EDITOR
        [SerializeField] private bool drawGizmos;
#endif
        private IBoundsProvider[] providers;

        private NativeArray<AabbNode> nodes;
        private NativeArray<int> indices;
        private NativeArray<Aabb> leafBounds;
        private NativeArray<AabbNodeData> nodeData;
        private NativeReference<int> rootIndexRef;
        private NativeReference<int> nodeCountRef;

        public override int DataCount => 0;

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!drawGizmos || !nodes.IsCreated) return;

            ReadOnlySpan<AabbNode> span = nodes.AsReadOnlySpan();
            int nodeCount = nodeCountRef.Value;
            int rootHeight = span[rootIndexRef.Value].Height;

            for (int i = 0; i < nodeCount; i++)
            {
                ref readonly AabbNode node = ref span[i];
                if (node.IsLeaf)
                {
                    Gizmos.color = Color.white;
                    Gizmos.DrawWireCube(node.Bounds.Center(), node.Bounds.Size());
                    Color leafCol = Color.white;
                    leafCol.a *= 0.01f;
                    Gizmos.color = leafCol;
                    Gizmos.DrawCube(node.Bounds.Center(), node.Bounds.Size());
                }
                else
                {
                    float colorIntensity = 1f - (node.Height - 1f) / rootHeight;
                    Color color = Color.HSVToRGB(node.Height / 10f % 1, 1, 1);
                    Gizmos.color = color * colorIntensity;
                    Gizmos.DrawWireCube(node.Bounds.Center(), node.Bounds.Size());
                }
            }
        }
#endif
        public override void InitializeBuffer(ref Material material, IBoundsProvider[] dataProviders)
        {
            if (IsInitialized)
                ReleaseBuffer();

            providers = dataProviders;
            int dataCount = providers.Length;
            int nodeCount = 2 * dataCount - 1;

            Buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, nodeCount, Stride);
            material.SetBuffer(BufferId, Buffer);

            nodes = new NativeArray<AabbNode>(nodeCount, Allocator.Persistent);
            indices = new NativeArray<int>(dataCount, Allocator.Persistent);
            leafBounds = new NativeArray<Aabb>(dataCount, Allocator.Persistent);
            nodeData = new NativeArray<AabbNodeData>(nodeCount, Allocator.Persistent);

            for (int i = 0; i < dataCount; i++)
            {
                indices[i] = i;
                leafBounds[i] = providers[i].GetBounds();
            }

            rootIndexRef = new NativeReference<int>(Allocator.Persistent);
            nodeCountRef = new NativeReference<int>(Allocator.Persistent);

            var job = new BvhBulkBuildJob
            {
                Nodes = nodes,
                Indices = indices,
                LeafBounds = leafBounds,
                RootIndexRef = rootIndexRef,
                NodeCountRef = nodeCountRef
            };
            job.Execute();
        }

        public override void SetData()
        {
            if (!IsInitialized) return;

            for (int i = 0; i < providers.Length; i++)
                leafBounds[i] = providers[i].GetBounds();

            int nodeCount = nodeCountRef.Value;
            int rootIndex = rootIndexRef.Value;

            BvhUtils.Refit(nodes, leafBounds, nodeCount);

            if (!nodeData.IsCreated || nodeData.Length != nodeCount)
            {
                if (nodeData.IsCreated)
                    nodeData.Dispose();
                nodeData = new NativeArray<AabbNodeData>(nodeCount, Allocator.Persistent);
            }
            BvhUtils.Flatten(nodes, rootIndex, nodeCount, nodeData);

            Buffer.SetData(nodeData);
        }

        public override void ReleaseBuffer()
        {
            Buffer?.Release();
            providers = null;

            if (nodes.IsCreated) nodes.Dispose();
            if (indices.IsCreated) indices.Dispose();
            if (nodeCountRef.IsCreated) nodeCountRef.Dispose();
            if (rootIndexRef.IsCreated) rootIndexRef.Dispose();

            if (leafBounds.IsCreated) leafBounds.Dispose();
            if (nodeData.IsCreated) nodeData.Dispose();
        }
    }
}
