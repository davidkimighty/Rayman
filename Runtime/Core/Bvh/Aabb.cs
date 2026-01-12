using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace Rayman
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Aabb
    {
        public float3 Min;
        public float3 Max;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Aabb(float3 min, float3 max)
        {
            Min = min;
            Max = max;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Aabb Union(Aabb a, Aabb b)
        {
            return new Aabb(math.min(a.Min, b.Min), math.max(a.Max, b.Max));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Encapsulate(float3 point)
        {
            Min = math.min(Min, point);
            Max = math.max(Max, point);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Expand(float amount)
        {
            float half = amount * 0.5f;
            Min -= half;
            Max += half;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float3 Center()
        {
            return math.mad(Max - Min, 0.5f, Min);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float3 Size()
        {
            return Max - Min;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float HalfArea()
        {
            float3 d = Max - Min;
            return d.x * d.y + d.x * d.z + d.y * d.z;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(float3 p)
        {
            return math.all(p >= Min & p <= Max);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Intersect(float3 rayOrigin, float3 invRayDir, float tMin, float tMax)
        {
            float3 t0 = (Min - rayOrigin) * invRayDir;
            float3 t1 = (Max - rayOrigin) * invRayDir;
            float3 tNear = math.min(t0, t1);
            float3 tFar = math.max(t0, t1);
            float enter = math.cmax(math.float4(tNear, tMin));
            float exit = math.cmin(math.float4(tFar, tMax));
            return enter <= exit;
        }
    }
}