using UnityEngine;

namespace Rayman
{
    public interface IBounds<T> where T : struct, IBounds<T>
    {
        bool Contains(T other);
        bool Intersects(T other);
        float HalfArea();
        Vector3 Center();
        Vector3 Extents();
        T Expand(float size);
        T Include(Vector3 point);
        T Union(T other);
    }

    public struct AABB : IBounds<AABB>
    {
        public Vector3 Min;
        public Vector3 Max;

        public AABB(Vector3 min, Vector3 max)
        {
            Min = min;
            Max = max;
        }

        public bool Contains(AABB aabb)
        {
            return Min.x <= aabb.Min.x && Min.y <= aabb.Min.y &&
                   Min.z <= aabb.Min.z && Max.x >= aabb.Max.x &&
                   Max.y >= aabb.Max.y && Max.z >= aabb.Max.z;
        }

        public bool Intersects(AABB aabb)
        {
            return Min.x <= aabb.Max.x && Max.x >= aabb.Min.x && 
                   Min.y <= aabb.Max.y && Max.y >= aabb.Min.y &&
                   Min.z <= aabb.Max.z && Max.z >= aabb.Min.z;
        }

        public float HalfArea()
        {
            Vector3 d = Max - Min;
            return d.x * d.y + d.y * d.z + d.z * d.x;
        }

        public Vector3 Center() => 0.5f * (Min + Max);

        public Vector3 Extents() => Max - Min;

        public AABB Expand(float size)
        {
            Vector3 expandSize = Vector3.one * size;
            return new AABB
            {
                Min = Min - expandSize,
                Max = Max + expandSize
            };
        }

        public AABB Include(Vector3 point)
        {
            return new AABB
            {
                Min = Vector3.Min(Min, point),
                Max = Vector3.Max(Max, point)
            };
        }

        public AABB Union(AABB aabb)
        {
            return new AABB
            {
                Min = Vector3.Min(Min, aabb.Min),
                Max = Vector3.Max(Max, aabb.Max)
            };
        }
    }
}