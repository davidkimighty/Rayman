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

    public struct BoundsConfig
    {
        public Transform Transform;
        public Vector3 Scale;
        public Vector3 Size;
        public Vector3 Pivot;

        public BoundsConfig(Transform transform, Vector3 scale, Vector3 size, Vector3 pivot)
        {
            Transform = transform;
            Scale = scale;
            Size = size;
            Pivot = pivot;
        }
    }
}