using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Rayman
{
    [BurstCompile]
    public struct BvhBulkBuildJob : IJob
    {
        public NativeArray<AabbNode> Nodes;
        public NativeArray<int> Indices;
        [ReadOnly] public NativeArray<Aabb> LeafBounds;
        public NativeReference<int> RootIndexRef;
        public NativeReference<int> NodeCountRef;

        public void Execute()
        {
            BuildBulk();
        }

        private void BuildBulk()
        {
            int n = LeafBounds.Length;
            if (n == 0) return;

            int nextFreeNode = 0;
            int stackPtr = 0;
            var stack = new NativeArray<(int start, int count, int nodeIdx)>(n * 2, Allocator.Temp);

            int rootIndex = nextFreeNode++;
            stack[stackPtr++] = (0, n, rootIndex);

            Span<AabbNode> nodesSpan = Nodes.AsSpan();
            Span<int> indexSpan = Indices.AsSpan();

            while (stackPtr > 0)
            {
                (int start, int count, int nodeIdx) = stack[--stackPtr];

                Aabb totalBounds = LeafBounds[indexSpan[start]];
                for (int i = 1; i < count; i++)
                    totalBounds = Aabb.Union(totalBounds, LeafBounds[indexSpan[start + i]]);

                if (count == 1)
                {
                    nodesSpan[nodeIdx] = new AabbNode(totalBounds)
                    {
                        LeftChild = ~indexSpan[start],
                        RightChild = -1,
                        Height = 0
                    };
                    continue;
                }

                float3 size = totalBounds.Size();
                int axis = size.y > size.x ? 1 : 0;
                if (size.z > (axis == 0 ? size.x : size.y))
                    axis = 2;

                float splitPos = totalBounds.Min[axis] + size[axis] * 0.5f;
                int midRel = Partition(indexSpan.Slice(start, count), LeafBounds, axis, splitPos);
                if (midRel == 0 || midRel == count)
                    midRel = count / 2;

                int leftIdx = nextFreeNode++;
                int rightIdx = nextFreeNode++;

                ref AabbNode node = ref nodesSpan[nodeIdx];
                node.Bounds = totalBounds;
                node.LeftChild = leftIdx;
                node.RightChild = rightIdx;

                stack[stackPtr++] = (start + midRel, count - midRel, rightIdx);
                stack[stackPtr++] = (start, midRel, leftIdx);
            }

            for (int i = nextFreeNode - 1; i >= 0; i--)
            {
                ref AabbNode node = ref nodesSpan[i];
                if (!node.IsLeaf)
                {
                    int l = node.LeftChild;
                    int r = node.RightChild;
                    node.Height = 1 + math.max(nodesSpan[l].Height, nodesSpan[r].Height);
                    node.Bounds = Aabb.Union(nodesSpan[l].Bounds, nodesSpan[r].Bounds);
                }
            }

            RootIndexRef.Value = rootIndex;
            NodeCountRef.Value = nextFreeNode;
            stack.Dispose();
        }

        private int Partition(Span<int> indices, NativeArray<Aabb> leafBounds, int axis, float splitPos)
        {
            int i = 0, j = indices.Length - 1;
            while (i <= j)
            {
                if (leafBounds[indices[i]].Center()[axis] < splitPos) i++;
                else
                {
                    (indices[i], indices[j]) = (indices[j], indices[i]);
                    j--;
                }
            }
            return i;
        }
    }

    [BurstCompile]
    public struct BvhRefitJob : IJob
    {
        public NativeArray<AabbNode> Nodes;
        [ReadOnly] public NativeArray<Aabb> LeafBounds;
        public int NodeCount;

        public void Execute()
        {
            BvhUtils.Refit(Nodes, LeafBounds, NodeCount);
        }
    }

    [BurstCompile]
    public struct BvhFlattenJob : IJob
    {
        [ReadOnly] public NativeArray<AabbNode> Nodes;
        [ReadOnly] public int NodeCount;
        [ReadOnly] public int RootIndex;

        public NativeArray<AabbNodeData> FlatResult;

        public void Execute()
        {
            BvhUtils.Flatten(Nodes, RootIndex, NodeCount, FlatResult);
        }
    }
}
