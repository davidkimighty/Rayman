using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

namespace Rayman
{
    public class ColorBufferProvider : IBufferProvider<ColorData>
    {
        public static int BufferId = Shader.PropertyToID("_ColorBuffer");
        public static readonly int Stride = UnsafeUtility.SizeOf<ColorData>();

        public GraphicsBuffer Buffer { get; private set; }

        public bool IsInitialized => Buffer != null;

        public void InitializeBuffer(ref Material material, ColorData[] data)
        {
            if (IsInitialized)
                ReleaseBuffer();

            int count = data.Length;
            Buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, count, Stride);
            material.SetBuffer(BufferId, Buffer);
        }

        public void SetData(ColorData[] data)
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
    public struct ColorData
    {
        public float4 Color;

        public ColorData(ShapeProvider provider)
        {
            Color = (Vector4)provider.Color;
        }
    }
}
