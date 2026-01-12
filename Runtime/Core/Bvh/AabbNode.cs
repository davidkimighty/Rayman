using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace Rayman
{
    [StructLayout(LayoutKind.Sequential)]
    public struct AabbNode
    {
        public Aabb Bounds;
        public int Parent;
        public int LeftChild;
        public int RightChild;
        public int Height;
        public int LeafId;

        public bool IsLeaf => LeafId != -1;

        public AabbNode(Aabb bounds, int leafId)
        {
            Bounds = bounds;
            Parent = -1;
            LeftChild = -1;
            RightChild = -1;
            Height = 0;
            LeafId = leafId;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct AabbNodeData
    {
        public float3 Min;
        public float3 Max;
        public int LeftChild;
        public int LeafId;

        public AabbNodeData(AabbNode node, int leftChild)
        {
            Min = node.Bounds.Min;
            Max = node.Bounds.Max;
            LeftChild = leftChild;
            LeafId = node.LeafId;
        }
    }
}
