using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

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
            {
                KnotProvider knot = providers[i];
                if (knot.TangentMode == TangentMode.Auto)
                {
                    Vector3 prevPos = knot.PreviousKnot.transform.position;
                    Vector3 nextPos = knot.NextKnot.transform.position;
                    Vector3 currentPos = knot.transform.position;
                    Vector3 autoTangent = SplineUtility.GetAutoSmoothTangent(prevPos, currentPos, nextPos);
                    knot.TangentOut = autoTangent;
                    knot.TangentIn = -autoTangent;
                }
                else if (knot.TangentMode == TangentMode.Linear)
                {
                    knot.TangentOut = Vector3.zero;
                    knot.TangentIn = Vector3.zero;
                }
                knotData[i] = new KnotData(knot);
            }
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