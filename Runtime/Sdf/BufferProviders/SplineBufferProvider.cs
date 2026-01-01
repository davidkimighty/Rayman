using System.Runtime.InteropServices;
using UnityEngine;

namespace Rayman
{
    public class SplineBufferProvider : BufferProvider<Spline>
    {
        public static int BufferId = Shader.PropertyToID("_SplineBuffer");
        
        private Spline[] providers;
        private SplineData[] splineData;
        
        public override void InitializeBuffer(ref Material material, Spline[] dataProviders)
        {
            if (IsInitialized)
                ReleaseBuffer();
            providers = dataProviders;
            
            int count = providers.Length;
            Buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, count, Marshal.SizeOf<SplineData>());
            material.SetBuffer(BufferId, Buffer);
            
            splineData = new SplineData[count];
            for (int i = 0; i < splineData.Length; i++)
                splineData[i] = new SplineData(providers[i]);
            Buffer.SetData(splineData);
        }

        public override void SetData()
        {
            if (!IsInitialized) return;
            
            for (int i = 0; i < splineData.Length; i++)
                splineData[i] = new SplineData(providers[i]);
            Buffer.SetData(splineData);
        }

        public override void ReleaseBuffer()
        {
            Buffer?.Release();
            Buffer = null;
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