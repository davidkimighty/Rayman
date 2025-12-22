using System;
using System.Collections.Generic;
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
    }

    public class ShapeProvider : MonoBehaviour, IBoundsProvider
    {
        public static Dictionary<Type, Delegate> BoundFactories = new();

        public Vector3 Size = Vector3.one * 0.5f;
        public float ExpandBounds = 0.001f;
        public Vector3 Pivot = Vector3.one * 0.5f;

        public OperationType Operation = OperationType.Union;
        [Range(0, 1f)] public float Blend;
        [Range(0, 1f)] public float Roundness;
        public ShapeType Shape = ShapeType.Sphere;

        [HideInInspector] public int GroupIndex;

        static ShapeProvider()
        {
            BoundFactories.Add(typeof(Aabb), (Func<BoundsConfig, Aabb>)Aabb.Create);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            Size = Vector3.Max(Size, Vector3.zero);
            Pivot = Pivot.Clamp(0f, 1f);
        }
#endif

        public T GetBounds<T>() where T : struct, IBounds<T>
        {
            if (!BoundFactories.TryGetValue(typeof(T), out Delegate factory)) return default;

            var factoryT = factory as Func<BoundsConfig, T>;
            if (factoryT == null) return default;

            T bounds = factoryT(new BoundsConfig(transform, GetScale(), GetSize(), Pivot));
            return bounds.Expand(Blend + Roundness + ExpandBounds);
        }

        public Vector3 GetScale() => transform.lossyScale;

        public Vector3 GetSize() => GetShapeSize(Shape, Size);

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

                default:
                    return size;
            }
        }
    }
}
