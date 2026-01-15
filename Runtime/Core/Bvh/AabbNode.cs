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

        public bool IsLeaf => LeftChild < 0;

        public AabbNode(Aabb bounds)
        {
            Bounds = bounds;
            Parent = -1;
            LeftChild = -1;
            RightChild = -1;
            Height = 0;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct AabbNodeData
    {
        public float3 Min;
        public float3 Max;
        public int LeftChild;

        public AabbNodeData(AabbNode node, int leftChild)
        {
            Min = node.Bounds.Min;
            Max = node.Bounds.Max;
            LeftChild = leftChild;
        }
    }
}
