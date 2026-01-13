using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Rayman
{
    public class GroupBufferProvider : IBufferProvider<ShapeGroup>
    {
        public static readonly int BufferId = Shader.PropertyToID("_GroupBuffer");
        public static readonly int Stride = UnsafeUtility.SizeOf<GroupData>();

        private GroupData[] groupData;

        public GraphicsBuffer Buffer { get; private set; }

        public bool IsInitialized => Buffer != null;

        public void InitializeBuffer(ref Material material, ShapeGroup[] data)
        {
            if (IsInitialized)
                ReleaseBuffer();

            int count = data.Length;
            groupData = new GroupData[count];

            Buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, count, Stride);
            material.SetBuffer(BufferId, Buffer);
        }

        public void SetData(ShapeGroup[] data)
        {
            if (!IsInitialized) return;

            for (int i = 0; i < data.Length; i++)
                groupData[i] = new GroupData(data[i]);

            Buffer.SetData(groupData);
        }

        public void ReleaseBuffer()
        {
            Buffer?.Release();
            groupData = null;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct GroupData
    {
        public int Operation;
        public float Blend;

        public GroupData(ShapeGroup group)
        {
            Operation = (int)group.Operation;
            Blend = group.Blend;
        }
    }
}
