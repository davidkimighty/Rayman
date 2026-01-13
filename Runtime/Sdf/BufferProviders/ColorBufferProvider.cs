using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

namespace Rayman
{
    public class ColorBufferProvider<T> : IBufferProvider<ShapeProvider>
        where T : struct, IPopulateData<ShapeProvider>
    {
        public static int BufferId = Shader.PropertyToID("_ColorBuffer");
        public static readonly int Stride = UnsafeUtility.SizeOf<T>();

        private T[] colorData;

        public GraphicsBuffer Buffer { get; private set; }

        public bool IsInitialized => Buffer != null;

        public void InitializeBuffer(ref Material material, ShapeProvider[] data)
        {
            if (IsInitialized)
                ReleaseBuffer();

            int count = data.Length;
            colorData = new T[count];

            Buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, count, Stride);
            material.SetBuffer(BufferId, Buffer);
        }

        public void SetData(ShapeProvider[] data)
        {
            if (!IsInitialized) return;

            for (int i = 0; i < data.Length; i++)
            {
                ShapeProvider provider = data[i];
                if (!provider) continue;

                colorData[i] = new T();
                colorData[i].Populate(provider);
            }

            Buffer.SetData(colorData);
        }

        public void ReleaseBuffer()
        {
            Buffer?.Release();
            colorData = null;
        }
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct ColorData : IPopulateData<ShapeProvider>
    {
        public float4 Color;

        public void Populate(ShapeProvider provider)
        {
            Color = (Vector4)provider.Color;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct GradientColorData : IPopulateData<ShapeProvider>
    {
        public float4 Color;
        public float4 GradientColor;
        public int UseGradient; // 4byte alignment

        public void Populate(ShapeProvider provider)
        {
            Color = (Vector4)provider.Color;
            GradientColor = (Vector4)provider.GradientColor;
            UseGradient = provider.UseGradient ? 1 : 0;
        }
    }
}
