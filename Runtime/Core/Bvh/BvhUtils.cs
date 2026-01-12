using System;
using Unity.Burst;
using Unity.Collections;

namespace Rayman
{
    public static class BvhUtils
    {
        [BurstCompile]
        public static void Refit(NativeArray<AabbNode> nodes, [ReadOnly] NativeArray<Aabb> leafBounds, int nodeCount)
        {
            Span<AabbNode> span = nodes.AsSpan();
            for (int i = nodeCount - 1; i >= 0; i--)
            {
                ref AabbNode node = ref span[i];
                if (node.IsLeaf)
                    node.Bounds = leafBounds[node.LeafId];
                else
                    node.Bounds = Aabb.Union(span[node.LeftChild].Bounds, span[node.RightChild].Bounds);
            }
        }

        [BurstCompile]
        public static void Flatten([ReadOnly] NativeArray<AabbNode> nodes, int rootIndex, int nodeCount, NativeArray<AabbNodeData> result)
        {
            if (rootIndex == -1 || nodeCount == 0) return;

            var flatPos = new NativeArray<int>(nodes.Length, Allocator.Temp);
            var stack = new NativeArray<int>(nodeCount, Allocator.Temp);
            var traversalOrder = new NativeArray<int>(nodeCount, Allocator.Temp);

            int stackPtr = 0;
            int flatCount = 0;
            stack[stackPtr++] = rootIndex;

            while (stackPtr > 0)
            {
                int index = stack[--stackPtr];
                traversalOrder[flatCount] = index;
                flatPos[index] = flatCount;
                flatCount++;

                ref readonly AabbNode node = ref nodes.AsReadOnlySpan()[index];
                if (!node.IsLeaf)
                {
                    stack[stackPtr++] = node.RightChild;
                    stack[stackPtr++] = node.LeftChild;
                }
            }

            for (int i = 0; i < nodeCount; i++)
            {
                int originalIndex = traversalOrder[i];
                ref readonly AabbNode node = ref nodes.AsReadOnlySpan()[originalIndex];
                int skipIndex = node.IsLeaf ? -1 : flatPos[node.RightChild] - i;
                result[i] = new AabbNodeData(node, skipIndex);
            }

            flatPos.Dispose();
            stack.Dispose();
            traversalOrder.Dispose();
        }

        public static float CalculateTotalCost(this NativeArray<AabbNode> nodes, int nodeCount)
        {
            ReadOnlySpan<AabbNode> span = nodes.AsReadOnlySpan();
            float totalArea = 0;
            for (int i = 0; i < nodeCount; i++)
            {
                ref readonly AabbNode node = ref span[i];
                if (!node.IsLeaf)
                    totalArea += node.Bounds.HalfArea();
            }
            return totalArea;
        }
    }
}
