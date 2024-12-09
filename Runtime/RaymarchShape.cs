using System;
using UnityEngine;

namespace Rayman
{
    public enum Shapes
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
        Cone,
        CappedCone,
    }

    public enum Combinations
    {
        Union,
        Subtract,
        Intersect
    }

    public enum Operations
    {
        None,
        Twist,
        Bend,
    }

    public class RaymarchShape : MonoBehaviour, IBoundsSource
    {
        [Serializable]
        public class Operation
        {
            public Operations Type;
            public float Amount;

            public bool Enabled => Type != Operations.None;
        }
        
        [Serializable]
        public class Setting
        {
            public Shapes Type;
            public Vector3 Size;
            [Range(0, 1f)] public float Roundness;
            public Combinations Combination;
            public float Smoothness;
            public Color Color;
            [ColorUsage(true, true)] public Color EmissionColor;
            [Range(0, 1f)] public float EmissionIntensity;
            public Operation Operation;
        }

        [SerializeField] private Setting settings;

        public Setting Settings => settings;

        public T GetBounds<T>() where T : struct, IBounds<T>
        {
            if (typeof(T) == typeof(AABB))
            {
                Vector3 half = settings.Size;
                Vector3 position = transform.position;
                return (T)(object)new AABB
                {
                    Min = position - half,
                    Max = position + half
                };
            }
            throw new InvalidOperationException($"Unsupported bounds type: {typeof(T)}");
        }
    }
}