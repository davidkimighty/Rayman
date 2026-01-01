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
        bool IsGroupDirty { get; set; }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct GroupData
    {
        public int Operation;
        public float Blend;

        public GroupData(IRaymarchGroup group)
        {
            Operation = (int)group.Operation;
            Blend = group.Blend;
        }
    }
}
