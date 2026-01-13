using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Rayman
{
    public class SplineBufferProvider : IBufferProvider<Spline>
    {
        public static int BufferId = Shader.PropertyToID("_SplineBuffer");
        public static readonly int Stride = UnsafeUtility.SizeOf<SplineData>();

        private SplineData[] splineData;

        public GraphicsBuffer Buffer { get; private set; }

        public bool IsInitialized => Buffer != null;

        public void InitializeBuffer(ref Material material, Spline[] data)
        {
            if (IsInitialized)
                ReleaseBuffer();

            int count = data.Length;
            splineData = new SplineData[count];

            Buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, count, Stride);
            material.SetBuffer(BufferId, Buffer);
        }

        public void SetData(Spline[] data)
        {
            if (!IsInitialized) return;

            for (int i = 0; i < data.Length; i++)
                splineData[i] = new SplineData(data[i]);

            Buffer.SetData(splineData);
        }

        public void ReleaseBuffer()
        {
            Buffer?.Release();
            splineData = null;
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