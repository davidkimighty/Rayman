using System;
using Unity.Collections;

namespace Rayman
{
    public static class BvhUtils
    {
        public static float CalculateTotalCost(this NativeArray<BvhNode> nodes, int nodeCount)
        {
            ReadOnlySpan<BvhNode> span = nodes.AsReadOnlySpan();
            float totalArea = 0;
            for (int i = 0; i < nodeCount; i++)
            {
                ref readonly BvhNode node = ref span[i];
                if (!node.IsLeaf)
                    totalArea += node.Bounds.HalfArea();
            }
            return totalArea;
        }
    }
}
