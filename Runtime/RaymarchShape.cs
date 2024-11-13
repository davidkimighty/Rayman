using System;
using UnityEngine;

namespace Rayman
{
    public enum Shapes
    {
        Sphere,
        Box,
        Capsule,
        Cylinder,
        Torus,
        Link,
    }

    public enum Operations
    {
        Union,
        Subtract,
        Intersect
    }

    public class RaymarchShape : MonoBehaviour
    {
        [Serializable]
        public class Setting
        {
            public Shapes Type;
            public Vector3 Size;
            public Operations Operation;
            public float Smoothness;
            public Color Color;
        }

        [SerializeField] private Setting _setting;

        public Setting ShapeSetting => _setting;
    }
}