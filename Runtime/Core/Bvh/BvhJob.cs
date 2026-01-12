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
        public NativeArray<BvhNode> Nodes;
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

            Span<BvhNode> nodesSpan = Nodes.AsSpan();
            Span<int> indexSpan = Indices.AsSpan();

            while (stackPtr > 0)
            {
                (int start, int count, int nodeIdx) = stack[--stackPtr];

                Aabb totalBounds = LeafBounds[indexSpan[start]];
                for (int i = 1; i < count; i++)
                    totalBounds = Aabb.Union(totalBounds, LeafBounds[indexSpan[start + i]]);

                if (count == 1)
                {
                    nodesSpan[nodeIdx] = new BvhNode(totalBounds, indexSpan[start]);
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

                ref BvhNode node = ref nodesSpan[nodeIdx];
                node.Bounds = totalBounds;
                node.LeftChild = leftIdx;
                node.RightChild = rightIdx;
                node.LeafId = -1;

                stack[stackPtr++] = (start + midRel, count - midRel, rightIdx);
                stack[stackPtr++] = (start, midRel, leftIdx);
            }

            for (int i = nextFreeNode - 1; i >= 0; i--)
            {
                ref BvhNode node = ref nodesSpan[i];
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
        public NativeArray<BvhNode> Nodes;
        [ReadOnly] public NativeArray<Aabb> LeafBounds;
        public int NodeCount;

        public void Execute()
        {
            Span<BvhNode> span = Nodes.AsSpan();
            for (int i = NodeCount - 1; i >= 0; i--)
            {
                ref BvhNode node = ref span[i];
                if (node.IsLeaf)
                    node.Bounds = LeafBounds[node.LeafId];
                else
                    node.Bounds = Aabb.Union(span[node.LeftChild].Bounds, span[node.RightChild].Bounds);
            }
        }
    }
}
