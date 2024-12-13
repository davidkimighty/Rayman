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
        private static readonly int DistortionCountId = Shader.PropertyToID("_DistortionCount");
        private static readonly int DistortionBufferId = Shader.PropertyToID("_DistortionBuffer");
        
        private static readonly int MaxStepsId = Shader.PropertyToID("_MaxSteps");
        private static readonly int MaxDistanceId = Shader.PropertyToID("_MaxDist");
        private static readonly int ShadowBiasId = Shader.PropertyToID("_ShadowBiasVal");
        
        [SerializeField] private Renderer _renderer;
        [SerializeField] private Material _matRef;
        [SerializeField] private RaymarchSetting _setting;
        [SerializeField] private List<RaymarchShape> _shapes = new();
        
        private Material _mat;
        private ComputeBuffer _shapeBuffer;
        private ComputeBuffer _distortionBuffer;
        private ShapeData[] _shapeData;
        private DistortionData[] _distortionData;

        private void Awake()
        {
            _mat = new Material(_matRef);
            _mat.SetInt(MaxStepsId, _setting.MaxSteps);
            _mat.SetFloat(MaxDistanceId, _setting.MaxDistance);
            _mat.SetFloat(ShadowBiasId, _setting.ShadowBias);
            _renderer.material = _mat;
            
            SetupShapeBuffer(_mat, _shapes.Count);
            InitOperationBuffer(_mat, _shapes.Count(s => s.Settings.Distortion.Enabled));
        }

        private void Update()
        {
            UpdateShapeData();
            UpdateOperationData();
        }

        private void SetupShapeBuffer(Material mat, int count)
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
            _distortionBuffer?.Release();
            if (count == 0) return;
            
            _distortionBuffer = new ComputeBuffer(count, DistortionData.Stride);
            _distortionData = new DistortionData[count];
            
            mat.SetInt(DistortionCountId, count);
            mat.SetBuffer(DistortionBufferId, _distortionBuffer);
            mat.EnableKeyword("_OPERATION_FEATURE");
        }

        private void UpdateShapeData()
        {
            if (_shapeBuffer == null || _shapeData == null) return;
            
            for (int i = 0; i < _shapeData.Length; i++)
            {
                RaymarchShape shape = _shapes[i];
                if (shape == null) continue;
                
                _shapeData[i] = new ShapeData
                {
                    Transform = shape.transform.worldToLocalMatrix,
                    LossyScale = shape.transform.lossyScale,
                    Type = (int)shape.Settings.Shape,
                    Size = shape.Settings.Size,
                    Roundness = shape.Settings.Roundness,
                    Combination = (int)shape.Settings.Operation,
                    Smoothness = shape.Settings.Smoothness,
                    Color = shape.Settings.Color,
                    EmissionColor = shape.Settings.EmissionColor,
                    EmissionIntensity = shape.Settings.EmissionIntensity,
                    DistortionEnabled = shape.Settings.Distortion.Enabled ? 1 : 0
                };
            }
            _shapeBuffer.SetData(_shapeData);
        }

        private void UpdateOperationData()
        {
            if (_distortionBuffer == null || _distortionData == null) return;

            int j = 0;
            for (int i = 0; i < _shapes.Count(); i++)
            {
                RaymarchShape.Distortion operation = _shapes[i].Settings.Distortion;
                if (!operation.Enabled) continue;
                
                _distortionData[j] = new DistortionData
                {
                    Id = i,
                    Type = (int)operation.Type,
                    Amount = operation.Amount
                };
                j++;
            }
            _distortionBuffer.SetData(_distortionData);
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
                
                SetupShapeBuffer(_mat, _shapes.Count);
                InitOperationBuffer(_mat, _shapes.Count(s => s.Settings.Distortion.Enabled));
                
                UpdateShapeData();
                UpdateOperationData();
            }
        }
        
        [ContextMenu("Reset Shape Buffer")]
        public void ResetShapeBuffer()
        {
            SetupShapeBuffer(_mat, _shapes.Count);
            InitOperationBuffer(_mat, _shapes.Count(s => s.Settings.Distortion.Enabled));
                
            UpdateShapeData();
            UpdateOperationData();
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
        public const int Stride = sizeof(float) * 33 + sizeof(int) * 3;

        public Matrix4x4 Transform;
        public Vector3 LossyScale;
        public int Type;
        public Vector3 Size;
        public float Roundness;
        public int Combination;
        public float Smoothness;
        public Vector4 Color;
        public Vector4 EmissionColor;
        public float EmissionIntensity;
        public int DistortionEnabled;
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct DistortionData
    {
        public const int Stride = sizeof(float) + sizeof(int) * 2;

        public int Id;
        public int Type;
        public float Amount;
    }
}