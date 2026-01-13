using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

namespace Rayman
{
    public class KnotBufferProvider : IBufferProvider<KnotData>
    {
        public static int BufferId = Shader.PropertyToID("_KnotBuffer");
        public static readonly int Stride = UnsafeUtility.SizeOf<KnotData>();

        public GraphicsBuffer Buffer { get; private set; }

        public bool IsInitialized => Buffer != null;

        public void InitializeBuffer(ref Material material, KnotData[] data)
        {
            if (IsInitialized)
                ReleaseBuffer();

            int count = data.Length;
            Buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, count, Stride);
            material.SetBuffer(BufferId, Buffer);
        }

        public void SetData(KnotData[] data)
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
    public struct KnotData
    {
        public float3 Position;
        public float3 TangentIn;
        public float3 TangentOut;
        public float Radius;
        public int SplineIndex;

        public KnotData(KnotProvider provider)
        {
            Position = provider.transform.position;
            TangentIn = provider.TangentIn;
            TangentOut = provider.TangentOut;
            Radius = provider.Radius;
            SplineIndex = provider.SplineIndex;
        }
    }
}