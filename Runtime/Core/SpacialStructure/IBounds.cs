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
}