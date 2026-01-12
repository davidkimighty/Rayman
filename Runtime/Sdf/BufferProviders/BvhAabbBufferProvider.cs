using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Rayman
{
    public class BvhAabbBufferProvider : BufferProvider<IBoundsProvider>
    {
        public static readonly int BufferId = Shader.PropertyToID("_NodeBuffer");
        
        [SerializeField] private float syncMargin = 0.01f;
#if UNITY_EDITOR
        [SerializeField] private bool drawGizmos;
#endif
        private IBoundsProvider[] providers;

        private NativeArray<BvhNode> nodes;
        private NativeArray<int> indices;
        private NativeReference<int> rootIndexRef;
        private NativeReference<int> nodeCountRef;

        private NativeArray<Aabb> boundsArray;
        private NativeArray<AabbNodeData> nodeDataArray;

        public override int DataCount => 0;

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!drawGizmos || !nodes.IsCreated) return;

            ReadOnlySpan<BvhNode> span = nodes.AsReadOnlySpan();
            int nodeCount = nodeCountRef.Value;
            int rootHeight = span[rootIndexRef.Value].Height;

            for (int i = 0; i < nodeCount; i++)
            {
                ref readonly BvhNode node = ref span[i];
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

            nodes = new NativeArray<BvhNode>(nodeCount, Allocator.Persistent);
            indices = new NativeArray<int>(dataCount, Allocator.Persistent);
            for (int i = 0; i < dataCount; i++)
                indices[i] = i;

            rootIndexRef = new NativeReference<int>(Allocator.Persistent);
            nodeCountRef = new NativeReference<int>(Allocator.Persistent);

            boundsArray = new NativeArray<Aabb>(dataCount, Allocator.Persistent);
            nodeDataArray = new NativeArray<AabbNodeData>(nodeCount, Allocator.Persistent);

            var job = new BvhBulkBuildJob
            {
                Nodes = nodes,
                Indices = indices,
                LeafBounds = boundsArray,
                RootIndexRef = rootIndexRef,
                NodeCountRef = nodeCountRef
            };
            job.Execute();

            Buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, nodeCount, Marshal.SizeOf<AabbNodeData>());
            material.SetBuffer(BufferId, Buffer);
        }

        public override void SetData()
        {
            if (!IsInitialized) return;

            for (int i = 0; i < providers.Length; i++)
                boundsArray[i] = providers[i].GetBounds();

            int nodeCount = nodeCountRef.Value;
            int rootIndex = rootIndexRef.Value;

            if (!nodeDataArray.IsCreated || nodeDataArray.Length != nodeCount)
            {
                if (nodeDataArray.IsCreated)
                    nodeDataArray.Dispose();
                nodeDataArray = new NativeArray<AabbNodeData>(nodeCount, Allocator.Persistent);
            }

            var refitJob = new BvhRefitJob
            {
                Nodes = nodes,
                LeafBounds = boundsArray,
                NodeCount = nodeCount
            };
            JobHandle jobHandle = refitJob.Schedule();

            var flattenJob = new BvhFlattenJob
            {
                Nodes = nodes,
                RootIndex = rootIndex,
                NodeCount = nodeCount,
                FlatResult = nodeDataArray
            };
            jobHandle = flattenJob.Schedule(jobHandle);
            jobHandle.Complete();

            Buffer.SetData(nodeDataArray);
        }

        public override void ReleaseBuffer()
        {
            Buffer?.Release();
            providers = null;

            if (nodes.IsCreated) nodes.Dispose();
            if (indices.IsCreated) indices.Dispose();
            if (nodeCountRef.IsCreated) nodeCountRef.Dispose();
            if (rootIndexRef.IsCreated) rootIndexRef.Dispose();

            if (boundsArray.IsCreated) boundsArray.Dispose();
            if (nodeDataArray.IsCreated) nodeDataArray.Dispose();
        }
    }

    public struct BvhFlattenJob : IJob
    {
        [ReadOnly] public NativeArray<BvhNode> Nodes;
        [ReadOnly] public int NodeCount;
        [ReadOnly] public int RootIndex;

        public NativeArray<AabbNodeData> FlatResult;

        public void Execute()
        {
            if (RootIndex == -1 || NodeCount == 0) return;

            var flatPos = new NativeArray<int>(Nodes.Length, Allocator.Temp);
            var stack = new NativeArray<int>(NodeCount, Allocator.Temp);
            var traversalOrder = new NativeArray<int>(NodeCount, Allocator.Temp);

            int stackPtr = 0;
            int flatCount = 0;
            stack[stackPtr++] = RootIndex;

            while (stackPtr > 0)
            {
                int index = stack[--stackPtr];
                traversalOrder[flatCount] = index;
                flatPos[index] = flatCount;
                flatCount++;

                ref readonly BvhNode node = ref Nodes.AsReadOnlySpan()[index];
                if (!node.IsLeaf)
                {
                    stack[stackPtr++] = node.RightChild;
                    stack[stackPtr++] = node.LeftChild;
                }
            }

            for (int i = 0; i < NodeCount; i++)
            {
                int originalIdx = traversalOrder[i];
                ref readonly BvhNode node = ref Nodes.AsReadOnlySpan()[originalIdx];

                int skipIndex = -1;
                if (!node.IsLeaf)
                    skipIndex = flatPos[node.RightChild] - i;

                FlatResult[i] = new AabbNodeData(node, skipIndex);
            }

            flatPos.Dispose();
            stack.Dispose();
            traversalOrder.Dispose();
        }
    }
}
