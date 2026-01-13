using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

namespace Rayman
{
    public class KnotBufferProvider : IBufferProvider<KnotProvider>
    {
        public const float DefaultTension = 1 / 3f;
        public static int BufferId = Shader.PropertyToID("_KnotBuffer");
        public static readonly int Stride = UnsafeUtility.SizeOf<KnotData>();

        private KnotData[] knotData;

        public GraphicsBuffer Buffer { get; private set; }

        public bool IsInitialized => Buffer != null;

        public void InitializeBuffer(ref Material material, KnotProvider[] data)
        {
            if (IsInitialized)
                ReleaseBuffer();

            int count = data.Length;
            knotData = new KnotData[count];

            Buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, count, Stride);
            material.SetBuffer(BufferId, Buffer);
        }

        public void SetData(KnotProvider[] data)
        {
            if (!IsInitialized) return;

            for (int i = 0; i < data.Length; i++)
            {
                KnotProvider knot = data[i];
                if (knot.TangentMode == TangentMode.Auto)
                {
                    Vector3 prevPos = knot.PreviousKnot.transform.position;
                    Vector3 nextPos = knot.NextKnot.transform.position;
                    Vector3 currentPos = knot.transform.position;
                    Vector3 autoTangent = GetAutoSmoothTangent(prevPos, currentPos, nextPos);
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

        public void ReleaseBuffer()
        {
            Buffer?.Release();
            knotData = null;
        }

        private float3 GetAutoSmoothTangent(float3 previous, float3 current, float3 next, float tension = DefaultTension)
        {
            var d1 = math.length(current - previous);
            var d2 = math.length(next - current);

            if (d1 == 0f)
                return (next - current) * 0.1f;
            else if (d2 == 0f)
                return (current - previous) * 0.1f;

            var a = tension;
            var twoA = 2f * tension;

            var d1PowA = math.pow(d1, a);
            var d1Pow2A = math.pow(d1, twoA);
            var d2PowA = math.pow(d2, a);
            var d2Pow2A = math.pow(d2, twoA);

            return (d1Pow2A * next - d2Pow2A * previous + (d2Pow2A - d1Pow2A) * current) / (3f * d1PowA * (d1PowA + d2PowA));
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