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
        T Expand(Vector3 size);
        T Include(Vector3 point);
        T Union(T other);
    }

    public struct Aabb : IBounds<Aabb>
    {
        public Vector3 Min;
        public Vector3 Max;

        public Aabb(Vector3 min, Vector3 max)
        {
            Min = min;
            Max = max;
        }

        public Aabb(Transform transform, Vector3 size, Vector3 scale, Vector3 Pivot)
        {
            Vector3 right = transform.right * (scale.x * size.x);
            Vector3 up = transform.up * (scale.y * size.y);
            Vector3 forward = transform.forward * (scale.z * size.z);

            Vector3 extent = new Vector3(
                Mathf.Abs(right.x) + Mathf.Abs(up.x) + Mathf.Abs(forward.x),
                Mathf.Abs(right.y) + Mathf.Abs(up.y) + Mathf.Abs(forward.y),
                Mathf.Abs(right.z) + Mathf.Abs(up.z) + Mathf.Abs(forward.z)
            );

            Vector3 offset = (Pivot - Vector3.one * 0.5f) * 2f;
            Vector3 rotatedOffset = right * offset.x + up * offset.y + forward * offset.z;
            Vector3 center = transform.position + rotatedOffset;

            Min = center - extent;
            Max = center + extent;
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