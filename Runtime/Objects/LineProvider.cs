using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Rayman
{
    public enum Lines
    {
        Segment,
        QuadraticBezier,
        CubicBezier,
    }
    
    public class LineProvider : MonoBehaviour, IBoundsProvider
    {
        [Header("Line")]
        public Lines Line = Lines.Segment;
        public Vector2 Radius = Vector2.one * 0.1f;
        public float ExpandBounds;
        public bool UseLossyScale = true;
        public List<Transform> Points = new();
        
        public Operations Operation = Operations.Union;
        [Range(0, 1f)] public float Blend;
        public Color Color = Color.white;
        public Color GradientColor = Color.white;
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            Radius = new Vector2(
                Mathf.Max(Radius.x, 0),
                Mathf.Max(Radius.y, 0));
        }
#endif
        
        public T GetBounds<T>() where T : struct, IBounds<T>
        {
            if (typeof(T) == typeof(Aabb))
            {
                Aabb aabb = new(transform.position, transform.position);
                foreach (Transform point in Points)
                    aabb = aabb.Include(point.position);
                aabb = aabb.Expand(Mathf.Max(Radius.x, Radius.y) + Blend + ExpandBounds + 0.001f);
                return (T)(object)aabb;
            }
            throw new InvalidOperationException($"Unsupported bounds type: {typeof(T)}");
        }
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct LineData : ISetupFromIndexed<LineProvider>
    {
        public int Type;
        public Matrix4x4 Transform;
        public int Operation;
        public float Blend;
        public Vector4 Radius;
        public int PointStartIndex;
        public int PointsCount;
        public Vector4 Color;

        public int Index
        {
            get => PointStartIndex;
            set => PointStartIndex = value;
        }

        public void SetupFrom(LineProvider data, int index)
        {
            Type = (int)data.Line;
            Transform = data.UseLossyScale ? data.transform.worldToLocalMatrix : 
                Matrix4x4.TRS(data.transform.position, data.transform.rotation, Vector3.one).inverse;
            Operation = (int)data.Operation;
            Blend = data.Blend;
            Radius = data.Radius;
            PointStartIndex = index;
            PointsCount = data.Points.Count;
            Color = data.Color;
        }
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct GradientLineData : ISetupFromIndexed<LineProvider>
    {
        public int Type;
        public Matrix4x4 Transform;
        public int Operation;
        public float Blend;
        public Vector4 Radius;
        public int PointStartIndex;
        public int PointsCount;
        public Vector4 Color;
        public Vector4 GradientColor;

        public int Index
        {
            get => PointStartIndex;
            set => PointStartIndex = value;
        }

        public void SetupFrom(LineProvider data, int index)
        {
            Type = (int)data.Line;
            Transform = data.UseLossyScale ? data.transform.worldToLocalMatrix : 
                Matrix4x4.TRS(data.transform.position, data.transform.rotation, Vector3.one).inverse;
            Operation = (int)data.Operation;
            Blend = data.Blend;
            Radius = data.Radius;
            PointStartIndex = index;
            PointsCount = data.Points.Count;
            Color = data.Color;
            GradientColor = data.GradientColor;
        }
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct PointData : ISetupFrom<Transform>
    {
        public Vector3 Position;

        public void SetupFrom(Transform data)
        {
            Position = data.localPosition;
        }
    }
}
