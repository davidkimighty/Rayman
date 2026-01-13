using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

namespace Rayman
{
    public class ShapeGroupBufferProvider : IBufferProvider<ShapeGroupData>
    {
        public static int BufferId = Shader.PropertyToID("_ShapeBuffer");
        public static readonly int Stride = UnsafeUtility.SizeOf<ShapeGroupData>();

        public GraphicsBuffer Buffer { get; private set; }

        public bool IsInitialized => Buffer != null;

        public void InitializeBuffer(ref Material material, ShapeGroupData[] data)
        {
            if (IsInitialized)
                ReleaseBuffer();

            int count = data.Length;
            Buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, count, Stride);
            material.SetBuffer(BufferId, Buffer);
        }

        public void SetData(ShapeGroupData[] data)
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
    public struct ShapeGroupData
    {
        public float3 Position;
        public Quaternion Rotation;
        public float3 Scale;
        public float3 Size;
        public float3 Pivot;
        public int Operation;
        public float Blend;
        public float Roundness;
        public int ShapeType;
        public int GroupIndex;

        public ShapeGroupData(ShapeProvider provider)
        {
            Position = provider.transform.position;
            Rotation = Quaternion.Inverse(provider.transform.rotation);
            Scale = provider.GetScale();
            Size = provider.Size;
            Pivot = provider.Pivot;
            Operation = (int)provider.Operation;
            Blend = provider.Blend;
            Roundness = provider.Roundness;
            ShapeType = (int)provider.Shape;
            GroupIndex = provider.GroupIndex;
        }
    }
}
