using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Rayman
{
    public class GroupBufferProvider : IBufferProvider<GroupData>
    {
        public static readonly int BufferId = Shader.PropertyToID("_GroupBuffer");
        public static readonly int Stride = UnsafeUtility.SizeOf<GroupData>();

        public GraphicsBuffer Buffer { get; private set; }

        public bool IsInitialized => Buffer != null;

        public void InitializeBuffer(ref Material material, GroupData[] data)
        {
            if (IsInitialized)
                ReleaseBuffer();

            int count = data.Length;
            Buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, count, Stride);
            material.SetBuffer(BufferId, Buffer);
        }

        public void SetData(GroupData[] data)
        {
            if (!IsInitialized) return;

            Buffer.SetData(data);
        }

        public void ReleaseBuffer()
        {
            Buffer?.Release();
        }
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
