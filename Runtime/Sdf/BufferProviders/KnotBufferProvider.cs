using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;

namespace Rayman
{
    public class KnotBufferProvider : BufferProvider<KnotProvider>
    {
        public static int BufferId = Shader.PropertyToID("_KnotBuffer");

        private KnotProvider[] providers;
        private KnotData[] knotData;
        
        public override void InitializeBuffer(ref Material material, KnotProvider[] dataProviders)
        {
            if (IsInitialized)
                ReleaseBuffer();
            providers = dataProviders;
            
            int count = providers.Length;
            Buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, count, Marshal.SizeOf<KnotData>());
            material.SetBuffer(BufferId, Buffer);
            
            knotData = new KnotData[count];
            for (int i = 0; i < knotData.Length; i++)
                knotData[i] = new KnotData(providers[i]);
            Buffer.SetData(knotData);
        }

        public override void SetData()
        {
            if (!IsInitialized) return;
            
            for (int i = 0; i < knotData.Length; i++)
                knotData[i] = new KnotData(providers[i]);
            Buffer.SetData(knotData);
        }

        public override void ReleaseBuffer()
        {
            Buffer?.Release();
            Buffer = null;
            knotData = null;
        }
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct KnotData
    {
        public float3 Position;
        public float3 TangentIn;
        public float3 TangentOut;
        public float Radius;
        public float Blend;
        public int SplineIndex;

        public KnotData(KnotProvider provider)
        {
            Position = provider.transform.position;
            TangentIn = provider.TangentIn;
            TangentOut = provider.TangentOut;
            Radius = provider.Radius;
            Blend = provider.Blend;
            SplineIndex = provider.SplineIndex;
        }
    }
}