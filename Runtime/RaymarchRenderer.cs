using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Rayman
{
    [ExecuteInEditMode]
    public class RaymarchRenderer : MonoBehaviour
    {
        private static readonly int ShapeCountId = Shader.PropertyToID("_ShapeCount");
        private static readonly int ShapeBufferId = Shader.PropertyToID("_ShapeBuffer");
        private static readonly int OperationCountId = Shader.PropertyToID("_OperationCount");
        private static readonly int OperationBufferId = Shader.PropertyToID("_OperationBuffer");
        
        private static readonly int MaxStepsId = Shader.PropertyToID("_MaxSteps");
        private static readonly int MaxDistanceId = Shader.PropertyToID("_MaxDist");
        private static readonly int ShadowBiasId = Shader.PropertyToID("_ShadowBiasVal");
        
        [SerializeField] private Renderer _renderer;
        [SerializeField] private Material _matRef;
        [SerializeField] private RaymarchSetting _setting;
        [SerializeField] private List<RaymarchShape> _shapes = new();
        
        private Material _mat;
        private ComputeBuffer _shapeBuffer;
        private ComputeBuffer _operationBuffer;
        private ShapeData[] _shapeData;
        private OperationData[] _operationData;

        private void Awake()
        {
            _mat = new Material(_matRef);
            _mat.SetInt(MaxStepsId, _setting.MaxSteps);
            _mat.SetFloat(MaxDistanceId, _setting.MaxDistance);
            _mat.SetFloat(ShadowBiasId, _setting.ShadowBias);
            _renderer.material = _mat;
            
            InitShapeBuffer(_mat, _shapes.Count);
            InitOperationBuffer(_mat, _shapes.Count(s => s.ShapeSetting.Operation.Enabled));
        }

        private void Update()
        {
            UpdateShapeBuffer();
            UpdateOperationBuffer();
        }

        private void InitShapeBuffer(Material mat, int count)
        {
            _shapeBuffer?.Release();
            if (count == 0) return;
            
            _shapeBuffer = new ComputeBuffer(count, ShapeData.Stride);
            _shapeData = new ShapeData[count];
        
            mat.SetInt(ShapeCountId, count);
            mat.SetBuffer(ShapeBufferId, _shapeBuffer);
        }

        private void InitOperationBuffer(Material mat, int count)
        {
            _operationBuffer?.Release();
            if (count == 0) return;
            
            _operationBuffer = new ComputeBuffer(count, OperationData.Stride);
            _operationData = new OperationData[count];
            
            mat.SetInt(OperationCountId, count);
            mat.SetBuffer(OperationBufferId, _operationBuffer);
            mat.EnableKeyword("_OPERATION_FEATURE");
        }

        private void UpdateShapeBuffer()
        {
            if (_shapeBuffer == null || _shapeData == null) return;
            
            for (int i = 0; i < _shapeData.Length; i++)
            {
                RaymarchShape shape = _shapes[i];
                Matrix4x4 matrix = Matrix4x4.TRS(shape.transform.position, shape.transform.rotation, shape.transform.lossyScale);
                
                _shapeData[i] = new ShapeData
                {
                    Transform = Matrix4x4.Inverse(matrix),
                    Type = (int)shape.ShapeSetting.Type,
                    Size = shape.ShapeSetting.Size,
                    Roundness = shape.ShapeSetting.Roundness,
                    Combination = (int)shape.ShapeSetting.Combination,
                    Smoothness = shape.ShapeSetting.Smoothness,
                    Color = shape.ShapeSetting.Color,
                    EmissionColor = shape.ShapeSetting.EmissionColor,
                    EmissionIntensity = shape.ShapeSetting.EmissionIntensity,
                    OperationEnabled = shape.ShapeSetting.Operation.Enabled ? 1 : 0
                };
            }
            _shapeBuffer.SetData(_shapeData);
        }

        private void UpdateOperationBuffer()
        {
            if (_operationBuffer == null || _operationData == null) return;

            int j = 0;
            for (int i = 0; i < _shapes.Count(); i++)
            {
                RaymarchShape.Operation operation = _shapes[i].ShapeSetting.Operation;
                if (!operation.Enabled) continue;
                
                _operationData[j] = new OperationData
                {
                    Id = i,
                    Type = (int)operation.Type,
                    Amount = operation.Amount
                };
                j++;
            }
            _operationBuffer.SetData(_operationData);
        }
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_mat != null)
            {
                _mat.SetInt(MaxStepsId, _setting.MaxSteps);
                _mat.SetFloat(MaxDistanceId, _setting.MaxDistance);
                _mat.SetFloat(ShadowBiasId, _setting.ShadowBias);
            }
        }

        private void OnGUI()
        {
            if (_shapeData == null)
            {
                if (_mat == null)
                {
                    _mat = new Material(_matRef);
                    _renderer.material = _mat;
                }
                
                InitShapeBuffer(_mat, _shapes.Count);
                InitOperationBuffer(_mat, _shapes.Count(s => s.ShapeSetting.Operation.Enabled));
                
                UpdateShapeBuffer();
                UpdateOperationBuffer();
            }
        }
        
        [ContextMenu("Reset Shape Buffer")]
        public void ResetShapeBuffer()
        {
            InitShapeBuffer(_mat, _shapes.Count);
            InitOperationBuffer(_mat, _shapes.Count(s => s.ShapeSetting.Operation.Enabled));
                
            UpdateShapeBuffer();
            UpdateOperationBuffer();
        }

        [ContextMenu("Find All Shapes")]
        private void FindAllShapes()
        {
            _shapes = Utilities.GetObjectsByTypes<RaymarchShape>(transform);
        }
#endif
    }
    
    [Serializable]
    public class RaymarchSetting
    {
        public int MaxSteps = 64;
        public float MaxDistance = 100f;
        public float ShadowBias = 0.1f;
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct ShapeData
    {
        public const int Stride = sizeof(float) * 30 + sizeof(int) * 3;

        public Matrix4x4 Transform;
        public int Type;
        public Vector3 Size;
        public float Roundness;
        public int Combination;
        public float Smoothness;
        public Vector4 Color;
        public Vector4 EmissionColor;
        public float EmissionIntensity;
        public int OperationEnabled;
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct OperationData
    {
        public const int Stride = sizeof(float) + sizeof(int) * 2;

        public int Id;
        public int Type;
        public float Amount;
    }
}