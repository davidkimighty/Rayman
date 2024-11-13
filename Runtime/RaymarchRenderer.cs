using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Rayman
{
    public class RaymarchRenderer : MonoBehaviour
    {
        private static readonly int ShapeCountId = Shader.PropertyToID("_ShapeCount");
        private static readonly int ShapeBufferId = Shader.PropertyToID("_ShapeBuffer");
        
        [SerializeField] private Renderer _renderer;
        [SerializeField] private List<RaymarchShape> _shapes = new();
        
        private ShapeData[] _shapesData;
        private ComputeBuffer _shapeBuffer;

        private void Awake()
        {
            Material mat = new Material(_renderer.material);
            _renderer.material = mat;
            InitShapeBuffer(mat, _shapes.Count);
        }

        private void LateUpdate()
        {
            UpdateShapeBuffer();
        }

        private void InitShapeBuffer(Material mat, int count)
        {
            _shapeBuffer?.Release();
            _shapeBuffer = new ComputeBuffer(count, ShapeData.Stride);
            _shapesData = new ShapeData[count];
        
            mat.SetInt(ShapeCountId, count);
            mat.SetBuffer(ShapeBufferId, _shapeBuffer);
        }

        private void UpdateShapeBuffer()
        {
            for (int i = 0; i < _shapesData.Length; i++)
            {
                RaymarchShape shape = _shapes[i];
                Matrix4x4 matrix = Matrix4x4.TRS(shape.transform.position, shape.transform.rotation, shape.transform.lossyScale);
                
                _shapesData[i] = new ShapeData
                {
                    Transform = Matrix4x4.Inverse(matrix),
                    Type = (int)shape.ShapeSetting.Type,
                    Size = shape.ShapeSetting.Size,
                    Operation = (int)shape.ShapeSetting.Operation,
                    Smoothness = shape.ShapeSetting.Smoothness,
                    Color = shape.ShapeSetting.Color,
                };
            }
            _shapeBuffer.SetData(_shapesData);
        }
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct ShapeData
    {
        public const int Stride = sizeof(float) * 16 + sizeof(int) * 2 + sizeof(float) * 8;

        public Matrix4x4 Transform;
        public int Type;
        public Vector3 Size;
        public int Operation;
        public float Smoothness;
        public Vector4 Color;
    }
}