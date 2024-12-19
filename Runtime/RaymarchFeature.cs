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
        public class Setting
        {
            [HideInInspector] public bool InitializeSettings;
#if RAYMARCH_DEBUG
            public DebugModes DebugMode = 0;
            public int BoundsDisplayThreshold = 50;
#endif
            public int MaxSteps = 64;
            public int MaxDistance = 100;

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
                if (!setting.InitializeSettings) return;
                
                cs.SetInt(ScreenWidthId, Camera.main.pixelWidth);
                cs.SetInt(ScreenHeightId, Camera.main.pixelHeight);
                cs.SetInt(RaymarchRenderer.MaxStepsId, setting.MaxSteps);
                cs.SetInt(RaymarchRenderer.MaxDistanceId, setting.MaxDistance);
#if RAYMARCH_DEBUG
                requiresIntermediateTexture = true;
                cs.SetInt("_DebugMode", (int)setting.DebugMode);
                cs.SetInt("_BoundsDisplayThreshold", setting.BoundsDisplayThreshold);
#endif
                setting.InitializeSettings = false;
            }
            
            public void SetupShapeResultBuffer(ShapeData[] shapeData)
            {
                int shapeCount = shapeData.Length;
                
                if (shapeBuffer == null || shapeBuffer.count != shapeCount)
                {
                    shapeBuffer?.Release();
                    shapeBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, shapeCount,
                        Marshal.SizeOf(typeof(ShapeData)));
                    
                    int totalThreads = Camera.main.pixelWidth * Camera.main.pixelHeight;
                    resultBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, totalThreads,
                        Marshal.SizeOf(typeof(ComputeResultData)));
                    // resultData = new ComputeResultData[totalThreads];
                    Shader.SetGlobalBuffer(ResultBufferId, resultBuffer);
                    Debug.Log($"ShapeBuffer & ResultBuffer initialized.");
                }
                shapeBuffer.SetData(shapeData);
            }

            public void SetupNodeBuffer(NodeData<AABB>[] nodeData)
            {
                int nodeCount = nodeData.Length;
                if (nodeBuffer == null || nodeBuffer.count != nodeCount)
                {
                    nodeBuffer?.Release();
                    nodeBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, nodeCount,
                        Marshal.SizeOf(typeof(NodeData<AABB>)));
                    Debug.Log($"NodeBuffer initialized.");
                }
                nodeBuffer.SetData(nodeData);
            }

            private static void ExecutePass(PassData data, ComputeGraphContext cgContext)
            {
                int mainKernel = data.Cs.FindKernel("CSMain");
                cgContext.cmd.SetComputeVectorParam(data.Cs, "_CameraPosition", Camera.main.transform.position);
                cgContext.cmd.SetComputeMatrixParam(data.Cs, "_CameraToWorld", Camera.main.cameraToWorldMatrix);
                cgContext.cmd.SetComputeMatrixParam(data.Cs, "_InverseProjectionMatrix", Camera.main.projectionMatrix.inverse);
                Matrix4x4 viewProjectionMatrix = Camera.main.projectionMatrix * Camera.main.worldToCameraMatrix;
                cgContext.cmd.SetComputeMatrixParam(data.Cs, "_ViewProjectionMatrix", viewProjectionMatrix);
                
                cgContext.cmd.SetComputeBufferParam(data.Cs, mainKernel, RaymarchRenderer.ShapeBufferId, data.ShapeBufferHandle);
                cgContext.cmd.SetComputeBufferParam(data.Cs, mainKernel, RaymarchRenderer.NodeBufferId, data.NodeBufferHandle);
                cgContext.cmd.SetComputeBufferParam(data.Cs, mainKernel, ResultBufferId, data.ResultBufferHandle);
#if RAYMARCH_DEBUG
                cgContext.cmd.SetComputeTextureParam(data.Cs, mainKernel, "_ResultTexture", data.ResultTextureHandle);
#endif
                int threadGroupsX = Mathf.CeilToInt(Camera.main.pixelWidth / 8.0f);
                int threadGroupsY = Mathf.CeilToInt(Camera.main.pixelHeight / 8.0f);
                cgContext.cmd.DispatchCompute(data.Cs, mainKernel, threadGroupsX, threadGroupsY, 1);
            }
        }
        
        private static readonly int ResultBufferId = Shader.PropertyToID("_ResultBuffer");
        private static readonly int ScreenWidthId = Shader.PropertyToID("_ScreenWidth");
        private static readonly int ScreenHeightId = Shader.PropertyToID("_ScreenHeight");

        public event Func<ShapeData[]> OnRequestShapeData;
        public event Func<NodeData<AABB>[]> OnRequestNodeData;
        
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
            setting.InitializeSettings = true;
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
                ShapeData[] shapeData = OnRequestShapeData?.Invoke();
                NodeData<AABB>[] nodeData = OnRequestNodeData?.Invoke();
                if (shapeData == null || nodeData == null) return;
                
                computePass.InitializePassSettings(setting);
                computePass.SetupShapeResultBuffer(shapeData);
                computePass.SetupNodeBuffer(nodeData);
                renderer.EnqueuePass(computePass);
            }
        }
        
#if RAYMARCH_DEBUG
        public void SetDebugMode(DebugModes mode)
        {
            if (setting.DebugMode == mode) return;
            
            setting.DebugMode = mode;
            setting.InitializeSettings = true;
        }

        public void SetBoundsDisplayThreshold(int threshold)
        {
            if (setting.BoundsDisplayThreshold == threshold) return;
            
            setting.BoundsDisplayThreshold = threshold;
            setting.InitializeSettings = true;
        }
#endif
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
