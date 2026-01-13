using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Rayman
{
    public class BvhBufferProvider : INativeBufferProvider<Aabb>
    {
        public static readonly int BufferId = Shader.PropertyToID("_NodeBuffer");
        public static readonly int Stride = UnsafeUtility.SizeOf<AabbNodeData>();

        private NativeArray<AabbNode> nodes;
        private NativeArray<int> indices;
        private NativeArray<AabbNodeData> nodeData;

        private NativeReference<int> rootIndexRef;
        private NativeReference<int> nodeCountRef;

        public GraphicsBuffer Buffer { get; private set; }

        public bool IsInitialized => Buffer != null && nodes.IsCreated;

        public int DataLength => nodeCountRef != null ? nodeCountRef.Value : 0;

        public void InitializeBuffer(ref Material material, NativeArray<Aabb> data)
        {
            if (IsInitialized)
                ReleaseBuffer();

            int dataCount = data.Length;
            int nodeCount = 2 * dataCount - 1;

            Buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, nodeCount, Stride);
            material.SetBuffer(BufferId, Buffer);

            nodes = new NativeArray<AabbNode>(nodeCount, Allocator.Persistent);
            indices = new NativeArray<int>(dataCount, Allocator.Persistent);
            nodeData = new NativeArray<AabbNodeData>(nodeCount, Allocator.Persistent);

            for (int i = 0; i < dataCount; i++)
                indices[i] = i;

            rootIndexRef = new NativeReference<int>(Allocator.Persistent);
            nodeCountRef = new NativeReference<int>(Allocator.Persistent);

            var job = new BvhBulkBuildJob
            {
                Nodes = nodes,
                Indices = indices,
                LeafBounds = data,
                RootIndexRef = rootIndexRef,
                NodeCountRef = nodeCountRef
            };
            job.Execute();
        }

        public void SetData(NativeArray<Aabb> data)
        {
            if (!IsInitialized) return;

            int nodeCount = nodeCountRef.Value;
            int rootIndex = rootIndexRef.Value;

            BvhUtils.Refit(nodes, data, nodeCount);

            if (!nodeData.IsCreated || nodeData.Length != nodeCount)
            {
                if (nodeData.IsCreated)
                    nodeData.Dispose();
                nodeData = new NativeArray<AabbNodeData>(nodeCount, Allocator.Persistent);
            }
            BvhUtils.Flatten(nodes, rootIndex, nodeCount, nodeData);

            Buffer.SetData(nodeData);
        }

        public void ReleaseBuffer()
        {
            Buffer?.Release();

            if (nodes.IsCreated) nodes.Dispose();
            if (indices.IsCreated) indices.Dispose();
            if (nodeData.IsCreated) nodeData.Dispose();
            if (nodeCountRef.IsCreated) nodeCountRef.Dispose();
            if (rootIndexRef.IsCreated) rootIndexRef.Dispose();
        }

#if UNITY_EDITOR
        public void DrawGizmos()
        {
            if (!nodes.IsCreated) return;

            ReadOnlySpan<AabbNode> span = nodes.AsReadOnlySpan();
            int nodeCount = this.nodeCountRef.Value;
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
    }
}
