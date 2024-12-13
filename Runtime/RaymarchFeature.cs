using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

namespace Rayman
{
    public class RaymarchFeature : ScriptableRendererFeature
    {
        [Serializable]
        public struct Setting
        {
            [HideInInspector] public bool ReinitSetting;
#if RAYMARCH_DEBUG
            [HideInInspector] public int DebugMode;
#endif
            public int MaxSteps;
            public int MaxDistance;

            public void SetTriggers(bool state)
            {
                ReinitSetting = state;
            }
        }
        
        public class RaymarchComputePass : ScriptableRenderPass
        {
            private class PassData
            {
                public ComputeShader Cs;
                public BufferHandle ShapeBufferHandle;
                public BufferHandle NodeBufferHandle;
                public BufferHandle ResultBufferHandle;
#if RAYMARCH_DEBUG
                public TextureHandle ResultTextureHandle;
#endif
            }
            
            private ComputeShader cs;
            private GraphicsBuffer shapeBuffer;
            private GraphicsBuffer nodeBuffer;
            private GraphicsBuffer resultBuffer;
            // private ComputeResultData[] resultData;

            public RaymarchComputePass(ComputeShader cs)
            {
                this.cs = cs;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                BufferHandle shapeBufferHandle = renderGraph.ImportBuffer(shapeBuffer);
                BufferHandle nodeBufferHandle = renderGraph.ImportBuffer(nodeBuffer);
                BufferHandle resultBufferHandle = renderGraph.ImportBuffer(resultBuffer);
#if RAYMARCH_DEBUG
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                TextureHandle source = resourceData.activeColorTexture;
                TextureDesc destinationDesc = renderGraph.GetTextureDesc(source);
                destinationDesc.name = "Raymarch Result Texture";
                destinationDesc.clearBuffer = false;
                destinationDesc.enableRandomWrite = true;
                TextureHandle destination = renderGraph.CreateTexture(destinationDesc);
                resourceData.cameraColor = destination;
#endif
                
                using (var builder = renderGraph.AddComputePass("Raymarch ComputePass", out PassData passData))
                {
                    passData.Cs = cs;
                    passData.ShapeBufferHandle = shapeBufferHandle;
                    passData.NodeBufferHandle = nodeBufferHandle;
                    passData.ResultBufferHandle = resultBufferHandle;
                    builder.UseBuffer(passData.ShapeBufferHandle);
                    builder.UseBuffer(passData.NodeBufferHandle);
                    builder.UseBuffer(passData.ResultBufferHandle, AccessFlags.Write);
#if RAYMARCH_DEBUG
                    passData.ResultTextureHandle = destination;
                    builder.UseTexture(passData.ResultTextureHandle, AccessFlags.Write);
#endif
                    builder.SetRenderFunc((PassData data, ComputeGraphContext cgContext) => ExecutePass(data, cgContext));
                }
            }
            
            public void InitializePassSettings(Setting setting)
            {
                if (!setting.ReinitSetting) return;
                
                cs.SetInt(ScreenWidthId, Camera.main.pixelWidth);
                cs.SetInt(ScreenHeightId, Camera.main.pixelHeight);
                cs.SetInt(RaymarchMaxStepsId, setting.MaxSteps);
                cs.SetInt(RaymarchMaxDistanceId, setting.MaxDistance);
#if RAYMARCH_DEBUG
                requiresIntermediateTexture = true;
                cs.SetInt("debugMode", setting.DebugMode);
#endif
            }
            
            public void SetupShapeResultBuffer(ComputeShapeData[] shapeData)
            {
                int shapeCount = shapeData.Length;
                cs.SetInt(ShapeCountId, shapeCount);
                
                if (shapeBuffer == null || shapeBuffer.count != shapeCount)
                {
                    shapeBuffer?.Release();
                    shapeBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, shapeCount, ComputeShapeData.Stride);
                    
                    int totalThreads = Camera.main.pixelWidth * Camera.main.pixelHeight;
                    resultBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, totalThreads, ComputeResultData.Stride);
                    // resultData = new ComputeResultData[totalThreads];
                    Shader.SetGlobalBuffer(ResultBufferId, resultBuffer);
                }
                shapeBuffer.SetData(shapeData);
            }

            public void SetupNodeBuffer(NodeData[] nodeData)
            {
                int nodeCount = nodeData.Length;
                if (nodeBuffer == null || nodeBuffer.count != nodeCount)
                {
                    nodeBuffer?.Release();
                    nodeBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, nodeCount, NodeData.Stride);
                }
                nodeBuffer.SetData(nodeData);
            }

            private static void ExecutePass(PassData data, ComputeGraphContext cgContext)
            {
                int mainKernel = data.Cs.FindKernel("CSMain");
                cgContext.cmd.SetComputeVectorParam(data.Cs, "cameraPosition", Camera.main.transform.position);
                cgContext.cmd.SetComputeMatrixParam(data.Cs, "cameraToWorld", Camera.main.cameraToWorldMatrix);
                cgContext.cmd.SetComputeMatrixParam(data.Cs, "inverseProjectionMatrix", Camera.main.projectionMatrix.inverse);
                Matrix4x4 viewProjectionMatrix = Camera.main.projectionMatrix * Camera.main.worldToCameraMatrix;
                cgContext.cmd.SetComputeMatrixParam(data.Cs, "viewProjectionMatrix", viewProjectionMatrix);
                
                cgContext.cmd.SetComputeBufferParam(data.Cs, mainKernel, ShapeBufferId, data.ShapeBufferHandle);
                cgContext.cmd.SetComputeBufferParam(data.Cs, mainKernel, NodeBufferId, data.NodeBufferHandle);
                cgContext.cmd.SetComputeBufferParam(data.Cs, mainKernel, ResultBufferId, data.ResultBufferHandle);
#if RAYMARCH_DEBUG
                cgContext.cmd.SetComputeTextureParam(data.Cs, mainKernel, "resultTexture", data.ResultTextureHandle);
#endif
                int threadGroupsX = Mathf.CeilToInt(Camera.main.pixelWidth / 8.0f);
                int threadGroupsY = Mathf.CeilToInt(Camera.main.pixelHeight / 8.0f);
                cgContext.cmd.DispatchCompute(data.Cs, mainKernel, threadGroupsX, threadGroupsY, 1);
            }
        }

        public const string DebugKeyword = "RAYMARCH_DEBUG";
        
        private static readonly int ShapeCountId = Shader.PropertyToID("shapeCount");
        private static readonly int ShapeBufferId = Shader.PropertyToID("shapeBuffer");
        private static readonly int NodeBufferId = Shader.PropertyToID("nodeBuffer");
        private static readonly int ResultBufferId = Shader.PropertyToID("resultBuffer");
        
        private static readonly int ScreenWidthId = Shader.PropertyToID("screenWidth");
        private static readonly int ScreenHeightId = Shader.PropertyToID("screenHeight");
        private static readonly int RaymarchMaxStepsId = Shader.PropertyToID("maxSteps");
        private static readonly int RaymarchMaxDistanceId = Shader.PropertyToID("maxDistance");

        public event Func<ComputeShapeData[]> OnRequestShapeData;
        public event Func<NodeData[]> OnRequestNodeData;
        
        [SerializeField] private ComputeShader raymarchCs;
#if UNITY_EDITOR
        [SerializeField] private ComputeShader debugCs;
#endif
        [SerializeField] private Setting setting;
        
        private RaymarchComputePass computePass;

        public override void Create()
        {
#if RAYMARCH_DEBUG
            if (debugCs == null) return;
            
            computePass = new RaymarchComputePass(debugCs);
            computePass.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
#else
            if (raymarchCs == null) return;

            computePass = new RaymarchComputePass(raymarchCs);
            computePass.renderPassEvent = RenderPassEvent.BeforeRenderingPrePasses;
#endif
            setting.SetTriggers(true);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (!SystemInfo.supportsComputeShaders)
            {
                Debug.LogWarning("Device does not support compute shaders.");
                return;
            }

            if (renderingData.cameraData.camera == Camera.main)
            {
                computePass.InitializePassSettings(setting);
                setting.SetTriggers(false);
                
                ComputeShapeData[] shapeData = OnRequestShapeData?.Invoke();
                NodeData[] nodeData = OnRequestNodeData?.Invoke();
                if (shapeData == null || nodeData == null) return;
                
                computePass.SetupShapeResultBuffer(shapeData);
                computePass.SetupNodeBuffer(nodeData);
                renderer.EnqueuePass(computePass);
            }
        }
        
#if RAYMARCH_DEBUG
        public void SetDebugMode(int mode)
        {
            setting.DebugMode = mode;
            setting.ReinitSetting = true;
        }
#endif
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct ComputeShapeData
    {
        public const int Stride = sizeof(float) * 33 + sizeof(int) * 4;

        public int GroupId;
        public int Id;
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

        public ComputeShapeData(int groupId, int id, Transform sourceTransform, RaymarchShape.Setting setting)
        {
            GroupId = groupId;
            Id = id;
            Transform = sourceTransform.worldToLocalMatrix;
            LossyScale = sourceTransform.lossyScale;
            Type = (int)setting.Shape;
            Size = setting.Size;
            Roundness = setting.Roundness;
            Combination = (int)setting.Operation;
            Smoothness = setting.Smoothness;
            Color = setting.Color;
            EmissionColor = setting.EmissionColor;
            EmissionIntensity = setting.EmissionIntensity;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct NodeData
    {
        public const int Stride = sizeof(float) * 6 + sizeof(int) * 4;

        public int Id;
        public AABB Bounds;
        public int Parent;
        public int Left;
        public int Right;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct ComputeResultData
    {
        public const int Stride = sizeof(float) * 15;

        public Vector3 HitPoint;
        public float TravelDistance;
        public float LastHitDistance;
        public Vector3 RayDirection;
        public Vector4 Color;
        public Vector3 Normal;
    }
}
