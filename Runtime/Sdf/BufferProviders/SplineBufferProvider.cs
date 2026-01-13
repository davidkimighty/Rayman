using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Rayman
{
    public class SplineBufferProvider : IBufferProvider<SplineData>
    {
        public static int BufferId = Shader.PropertyToID("_SplineBuffer");
        public static readonly int Stride = UnsafeUtility.SizeOf<SplineData>();

        public GraphicsBuffer Buffer { get; private set; }

        public bool IsInitialized => Buffer != null;

        public void InitializeBuffer(ref Material material, SplineData[] data)
        {
            if (IsInitialized)
                ReleaseBuffer();

            int count = data.Length;
            Buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, count, Stride);
            material.SetBuffer(BufferId, Buffer);
        }

        public void SetData(SplineData[] data)
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
    public struct SplineData
    {
        public int KnotStartIndex;
        public int KnotCount;

        public SplineData(Spline spline)
        {
            KnotStartIndex = spline.KnotStartIndex;
            KnotCount = spline.Knots.Count;
        }
    }
}