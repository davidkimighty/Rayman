using UnityEngine;

namespace Rayman
{
    public struct Aabb : IBounds<Aabb>
    {
        public Vector3 Min;
        public Vector3 Max;

        public Aabb(Vector3 a, Vector3 b)
        {
            Min = Vector3.Min(a, b);
            Max = Vector3.Max(a, b);
        }

        public bool Contains(Aabb aabb)
        {
            return Min.x <= aabb.Min.x && Min.y <= aabb.Min.y &&
                Min.z <= aabb.Min.z && Max.x >= aabb.Max.x &&
                Max.y >= aabb.Max.y && Max.z >= aabb.Max.z;
        }

        public bool Intersects(Aabb aabb)
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

        public Aabb Expand(float size)
        {
            Vector3 expandSize = Vector3.one * size;
            return new Aabb
            {
                Min = Min - expandSize,
                Max = Max + expandSize
            };
        }

        public Aabb Expand(Vector3 size)
        {
            return new Aabb
            {
                Min = Min - size,
                Max = Max + size
            };
        }

        public Aabb Include(Vector3 point)
        {
            return new Aabb
            {
                Min = Vector3.Min(Min, point),
                Max = Vector3.Max(Max, point)
            };
        }

        public Aabb Union(Aabb aabb)
        {
            return new Aabb
            {
                Min = Vector3.Min(Min, aabb.Min),
                Max = Vector3.Max(Max, aabb.Max)
            };
        }
    }
}