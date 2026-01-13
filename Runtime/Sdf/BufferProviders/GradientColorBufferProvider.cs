using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

namespace Rayman
{
    public class GradientColorBufferProvider : IBufferProvider<GradientColorData>
    {
        public static int BufferId = Shader.PropertyToID("_ColorBuffer");
        public static readonly int Stride = UnsafeUtility.SizeOf<GradientColorData>();

        public GraphicsBuffer Buffer { get; private set; }

        public bool IsInitialized => Buffer != null;

        public void InitializeBuffer(ref Material material, GradientColorData[] data)
        {
            if (IsInitialized)
                ReleaseBuffer();

            material.EnableKeyword("_GRADIENT_COLOR");

            int count = data.Length;
            Buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, count, Stride);
            material.SetBuffer(BufferId, Buffer);
        }

        public void SetData(GradientColorData[] data)
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
    public struct GradientColorData
    {
        public float4 Color;
        public float4 GradientColor;
        public int UseGradient; // 4byte alignment

        public GradientColorData(ColorProvider provider)
        {
            Color = (Vector4)provider.Color;
            GradientColor = (Vector4)provider.GradientColor;
            UseGradient = provider.UseGradient ? 1 : 0;
        }
    }
}