using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

namespace Rayman
{
    public class ComputeRaymarchFeature : ScriptableRendererFeature
    {
        public class RaymarchComputePass : ScriptableRenderPass
        {
            private class PassData
            {
                public ComputeShader Cs;
                public BufferHandle ShapeBufferHandle;
                public BufferHandle NodeBufferHandle;
                public BufferHandle ResultBufferHandle;
#if UNITY_EDITOR
                public TextureHandle ResultTextureHandle;
                public DebugModes DebugMode;
#endif
            }
            
            private ComputeShader cs;
            private RaymarchSetting setting;
            private GraphicsBuffer shapeBuffer;
            private GraphicsBuffer nodeBuffer;
            private GraphicsBuffer resultBuffer;
            // private ComputeResultData[] resultData;

            public RaymarchComputePass(ComputeShader cs, RaymarchSetting setting)
            {
                this.cs = cs;
                this.setting = setting;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                BufferHandle shapeBufferHandle = renderGraph.ImportBuffer(shapeBuffer);
                BufferHandle nodeBufferHandle = renderGraph.ImportBuffer(nodeBuffer);
                BufferHandle resultBufferHandle = renderGraph.ImportBuffer(resultBuffer);
#if UNITY_EDITOR
                TextureHandle destination = default;
                if (setting.DebugMode != DebugModes.None)
                {
                    UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                    TextureHandle source = resourceData.activeColorTexture;
                    TextureDesc destinationDesc = renderGraph.GetTextureDesc(source);
                    destinationDesc.name = "Raymarch Result Texture";
                    destinationDesc.clearBuffer = false;
                    destinationDesc.enableRandomWrite = true;
                    destination = renderGraph.CreateTexture(destinationDesc);
                    resourceData.cameraColor = destination;
                }
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
#if UNITY_EDITOR
                    if (setting.DebugMode != DebugModes.None)
                    {
                        passData.DebugMode = setting.DebugMode;
                        passData.ResultTextureHandle = destination;
                        builder.UseTexture(passData.ResultTextureHandle, AccessFlags.Write);
                    }
#endif
                    builder.SetRenderFunc((PassData data, ComputeGraphContext cgContext) => ExecutePass(data, cgContext));
                }
            }
            
            public void InitializePassSettings()
            {
                if (!setting.TriggerState) return;
                
                cs.SetInt(ScreenWidthId, Screen.width);
                cs.SetInt(ScreenHeightId,  Screen.height);
                cs.SetFloat(RenderScaleId, UniversalRenderPipeline.asset.renderScale);
                cs.SetInt(RaymarchRenderer.MaxStepsId, setting.MaxSteps);
                cs.SetFloat(RaymarchRenderer.MaxDistanceId, setting.MaxDistance);
#if UNITY_EDITOR
                if (setting.DebugMode != DebugModes.None)
                {
                    requiresIntermediateTexture = true;
                    cs.SetInt(RaymarchRenderer.DebugModeId, (int)setting.DebugMode);
                    cs.SetInt(RaymarchRenderer.BoundsDisplayThresholdId, setting.BoundsDisplayThreshold);
                }
#endif
                setting.SetTrigger(false);
                Debug.Log($"[Raymarch Feature] Raymarch settings initialized.");
            }
            
            public void SetupShapeResultBuffer(ShapeData[] shapeData)
            {
                if (shapeData == null) return;
                
                int shapeCount = shapeData.Length;
                if (shapeBuffer == null || shapeBuffer.count != shapeCount)
                {
                    shapeBuffer?.Release();
                    shapeBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, shapeCount,
                        Marshal.SizeOf(typeof(ShapeData)));
                    
                    int totalThreads = Screen.width * Screen.height;
                    resultBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, totalThreads,
                        Marshal.SizeOf(typeof(ComputeResultData)));
                    Shader.SetGlobalBuffer(ResultBufferId, resultBuffer);
                    // resultData = new ComputeResultData[totalThreads];
                    Debug.Log($"[Raymarch Feature] ShapeBuffer[{shapeCount}] & ResultBuffer[{totalThreads}] initialized.");
                }
                shapeBuffer.SetData(shapeData);
            }

            public void SetupNodeBuffer(NodeDataAABB[] nodeData)
            {
                if (nodeData == null) return;
                
                int nodeCount = nodeData.Length;
                if (nodeBuffer == null || nodeBuffer.count != nodeCount)
                {
                    nodeBuffer?.Release();
                    nodeBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, nodeCount,
                        Marshal.SizeOf(typeof(NodeDataAABB)));
                    Debug.Log($"[Raymarch Feature] NodeBuffer initialized.");
                }
                nodeBuffer.SetData(nodeData);
            }

            private static void ExecutePass(PassData data, ComputeGraphContext cgContext)
            {
                int mainKernel = data.Cs.FindKernel("CSMain");
                cgContext.cmd.SetComputeVectorParam(data.Cs, CameraPositionId, Camera.main.transform.position);
                cgContext.cmd.SetComputeMatrixParam(data.Cs, CameraToWorldId, Camera.main.cameraToWorldMatrix);
                cgContext.cmd.SetComputeMatrixParam(data.Cs, InverseProjectionMatrixId, Camera.main.projectionMatrix.inverse);
                Matrix4x4 viewProjectionMatrix = Camera.main.projectionMatrix * Camera.main.worldToCameraMatrix;
                cgContext.cmd.SetComputeMatrixParam(data.Cs, ViewProjectionMatrixId, viewProjectionMatrix);
                
                cgContext.cmd.SetComputeBufferParam(data.Cs, mainKernel, RaymarchRenderer.ShapeBufferId, data.ShapeBufferHandle);
                cgContext.cmd.SetComputeBufferParam(data.Cs, mainKernel, RaymarchRenderer.NodeBufferId, data.NodeBufferHandle);
                cgContext.cmd.SetComputeBufferParam(data.Cs, mainKernel, ResultBufferId, data.ResultBufferHandle);
#if UNITY_EDITOR
                if (data.DebugMode != DebugModes.None)
                    cgContext.cmd.SetComputeTextureParam(data.Cs, mainKernel, "_ResultTexture", data.ResultTextureHandle);
#endif
                int threadGroupsX = Mathf.CeilToInt(Screen.width / 8.0f);
                int threadGroupsY = Mathf.CeilToInt(Screen.height / 8.0f);
                cgContext.cmd.DispatchCompute(data.Cs, mainKernel, threadGroupsX, threadGroupsY, 1);
            }
        }
        
        private static readonly int CameraPositionId = Shader.PropertyToID("_CameraPosition");
        private static readonly int CameraToWorldId = Shader.PropertyToID("_CameraToWorld");
        private static readonly int InverseProjectionMatrixId = Shader.PropertyToID("_InverseProjectionMatrix");
        private static readonly int ViewProjectionMatrixId = Shader.PropertyToID("_ViewProjectionMatrix");
        private static readonly int RenderScaleId = Shader.PropertyToID("_RenderScale");
        private static readonly int ScreenWidthId = Shader.PropertyToID("_ScreenWidth");
        private static readonly int ScreenHeightId = Shader.PropertyToID("_ScreenHeight");
        private static readonly int ResultBufferId = Shader.PropertyToID("_ResultBuffer");
        
        public RaymarchSetting Setting = new();
        public IComputeRaymarchDataProvider PassDataProvider;
        
        [SerializeField] private ComputeShader raymarchCs;
#if UNITY_EDITOR
        [SerializeField] private ComputeShader debugCs;
#endif
        private RaymarchComputePass computePass;

        public override void Create()
        {
#if UNITY_EDITOR
            if (Setting.DebugMode != DebugModes.None)
            {
                if (debugCs == null) return;
                computePass = new RaymarchComputePass(debugCs, Setting);
                computePass.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
            }
#endif
            if (raymarchCs == null) return;

            if (computePass == null)
            {
                computePass = new RaymarchComputePass(raymarchCs, Setting);
                computePass.renderPassEvent = RenderPassEvent.AfterRenderingPrePasses;
            }
            Setting.SetTrigger();
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
                if (PassDataProvider == null) return;
                
                computePass.InitializePassSettings();
                computePass.SetupShapeResultBuffer(PassDataProvider.GetShapeData());
                computePass.SetupNodeBuffer(PassDataProvider.GetNodeData());
                renderer.EnqueuePass(computePass);
            }
        }
    }
    
    [Serializable]
    public class RaymarchSetting
    {
        public int MaxSteps = 64;
        public float MaxDistance = 100f;
        public int ShadowMaxSteps = 32;
        public float ShadowMaxDistance = 30f;
#if UNITY_EDITOR
        public DebugModes DebugMode = 0;
        public int BoundsDisplayThreshold = 50;
#endif
        private bool initializeSettings;

        public bool TriggerState => initializeSettings;

        public void SetTrigger(bool state = true) => initializeSettings = state;
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct ComputeResultData
    {
        public Vector3 HitPoint;
        public float TravelDistance;
        public float LastHitDistance;
        public Vector3 RayDirection;
        public Vector4 Color;
        public Vector3 Normal;
    }
}
