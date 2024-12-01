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

    public class RaymarchShape : MonoBehaviour
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

        [SerializeField] private Setting _setting;

        public Setting Settings => _setting;
    }
}