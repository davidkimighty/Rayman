using System.Runtime.InteropServices;

namespace Rayman
{
    public enum OperationType
    {
        Union,
        Subtract,
        Intersect
    }

    public interface IRaymarchGroup
    {
        OperationType Operation { get; }
        float Blend { get; }
        int Count { get; }
        bool IsGroupDirty { get; set; }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct GroupData
    {
        public int Operation;
        public float Blend;
        public int StartIndex;
        public int Count;

        public GroupData(IRaymarchGroup group, int startIndex)
        {
            Operation = (int)group.Operation;
            Blend = group.Blend;
            StartIndex = startIndex;
            Count = group.Count;
        }

        public GroupData(OperationType type, float blend, int startIndex, int count)
        {
            Operation = (int)type;
            Blend = blend;
            StartIndex = startIndex;
            Count = count;
        }
    }
}
