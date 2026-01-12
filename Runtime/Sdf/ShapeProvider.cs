using System;
using UnityEngine;

namespace Rayman
{
    public enum ShapeType
    {
        Sphere,
        Ellipsoid,
        Box,
        Octahedron,
        Capsule,
        Cylinder,
        Torus,
        CappedTorus,
        Link,
        CappedCone,
        Pyramid
    }

    public class ShapeProvider : MonoBehaviour, IBoundsProvider
    {
        public Vector3 Size = Vector3.one * 0.5f;
        public float ExpandBounds = 0.001f;
        public Vector3 Pivot = Vector3.one * 0.5f;

        public OperationType Operation = OperationType.Union;
        [Range(0, 1f)] public float Blend;
        [Range(0, 1f)] public float Roundness;
        public ShapeType Shape = ShapeType.Sphere;

        [HideInInspector] public int GroupIndex;

#if UNITY_EDITOR
        private void OnValidate()
        {
            Size = Vector3.Max(Size, Vector3.zero);
            Pivot = Pivot.Clamp(0f, 1f);
        }
#endif

        public Aabb GetBounds()
        {
            Aabb aabb = CreateAabb(transform, GetShapeSize(Shape, Size), GetScale(), Pivot);
            aabb.Expand(ExpandBounds);
            return aabb;
        }

        public Vector3 GetScale() => transform.lossyScale;
        
        private Aabb CreateAabb(Transform transform, Vector3 scale, Vector3 size, Vector3 pivot)
        {
            Vector3 right = transform.right * (scale.x * size.x);
            Vector3 up = transform.up * (scale.y * size.y);
            Vector3 forward = transform.forward * (scale.z * size.z);

            Vector3 extent = new Vector3(
                Mathf.Abs(right.x) + Mathf.Abs(up.x) + Mathf.Abs(forward.x),
                Mathf.Abs(right.y) + Mathf.Abs(up.y) + Mathf.Abs(forward.y),
                Mathf.Abs(right.z) + Mathf.Abs(up.z) + Mathf.Abs(forward.z)
            );

            Vector3 offset = (pivot - Vector3.one * 0.5f) * 2f;
            Vector3 rotatedOffset = right * offset.x + up * offset.y + forward * offset.z;
            Vector3 center = transform.position + rotatedOffset;

            Vector3 min = center - extent;
            Vector3 max = center + extent;
            return new Aabb(min, max);
        }

        private Vector3 GetShapeSize(ShapeType shape, Vector3 size)
        {
            switch (shape)
            {
                case ShapeType.Sphere:
                case ShapeType.Octahedron:
                    return Vector3.one * size.x;

                case ShapeType.Capsule:
                    return new Vector3(size.x, (size.y + size.x) * 0.5f + size.x * 0.5f, size.x);

                case ShapeType.Cylinder:
                    return new Vector3(size.x, size.y, size.x);

                case ShapeType.Torus:
                    return Vector3.one * (size.x + size.y);

                case ShapeType.CappedTorus:
                    return Vector3.one * (size.y + size.z);

                case ShapeType.Link:
                    float xz = size.x + size.z;
                    return new Vector3(xz, size.y + xz, xz);

                case ShapeType.CappedCone:
                    float max = Mathf.Max(size.x, size.z);
                    return new Vector3(max, size.y, max);
                
                case ShapeType.Pyramid:
                    return new Vector3(size.x, size.y * 0.5f, size.x);

                default:
                    return size;
            }
        }

        
    }
}
