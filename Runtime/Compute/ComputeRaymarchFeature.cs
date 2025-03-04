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
                public TextureHandle ResultTextureHandle;
            }
            
            private ComputeShader cs;
            private GraphicsBuffer shapeBuffer;
            private GraphicsBuffer nodeBuffer;

            public RaymarchComputePass(ComputeShader cs)
            {
                this.cs = cs;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                BufferHandle shapeBufferHandle = renderGraph.ImportBuffer(shapeBuffer);
                BufferHandle nodeBufferHandle = renderGraph.ImportBuffer(nodeBuffer);
                
                requiresIntermediateTexture = true;
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                TextureHandle source = resourceData.activeColorTexture;
                TextureDesc destinationDesc = renderGraph.GetTextureDesc(source);
                destinationDesc.name = "Raymarch Result Texture";
                destinationDesc.clearBuffer = false;
                destinationDesc.enableRandomWrite = true;
                
                TextureHandle destination = renderGraph.CreateTexture(destinationDesc);
                resourceData.cameraColor = destination;
                
                using (var builder = renderGraph.AddComputePass("Raymarch ComputePass", out PassData passData))
                {
                    passData.Cs = cs;
                    passData.ShapeBufferHandle = shapeBufferHandle;
                    passData.NodeBufferHandle = nodeBufferHandle;
                    builder.UseBuffer(passData.ShapeBufferHandle);
                    builder.UseBuffer(passData.NodeBufferHandle);

                    passData.ResultTextureHandle = destination;
                    builder.UseTexture(passData.ResultTextureHandle, AccessFlags.Write);
                    
                    builder.SetRenderFunc((PassData data, ComputeGraphContext cgContext) => ExecutePass(data, cgContext));
                }
            }
            
            public void SetRaymarchSettings(RaymarchSetting setting)
            {
                cs.SetInt(ScreenWidthId, Screen.width);
                cs.SetInt(ScreenHeightId,  Screen.height);
                cs.SetFloat(RenderScaleId, UniversalRenderPipeline.asset.renderScale);
                cs.SetInt(RayDataProvider.MaxStepsId, setting.MaxSteps);
                cs.SetFloat(RayDataProvider.MaxDistanceId, setting.MaxDistance);
                cs.SetInt(RayDataProvider.ShadowMaxStepsId, setting.ShadowMaxSteps);
                cs.SetFloat(RayDataProvider.ShadowMaxDistanceId, setting.ShadowMaxDistance);
            }
            
            public void SetupShapeResultBuffer(ColorShapeData[] shapeData)
            {
                if (shapeData == null) return;
                
                int shapeCount = shapeData.Length;
                if (shapeBuffer == null || shapeBuffer.count != shapeCount)
                {
                    shapeBuffer?.Release();
                    shapeBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, shapeCount,
                        Marshal.SizeOf(typeof(ColorShapeData)));
                }
                shapeBuffer.SetData(shapeData);
            }

            public void SetupNodeBuffer(AabbNodeData[] nodeData)
            {
                if (nodeData == null) return;
                
                int nodeCount = nodeData.Length;
                if (nodeBuffer == null || nodeBuffer.count != nodeCount)
                {
                    nodeBuffer?.Release();
                    nodeBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, nodeCount,
                        Marshal.SizeOf(typeof(AabbNodeData)));
                }
                nodeBuffer.SetData(nodeData);
            }

            private static void ExecutePass(PassData data, ComputeGraphContext cgContext)
            {
                Camera mainCamera = Camera.main;
                cgContext.cmd.SetComputeVectorParam(data.Cs, CameraPositionId, mainCamera.transform.position);
                cgContext.cmd.SetComputeMatrixParam(data.Cs, CameraToWorldId, mainCamera.cameraToWorldMatrix);
                cgContext.cmd.SetComputeMatrixParam(data.Cs, InverseProjectionMatrixId, mainCamera.projectionMatrix.inverse);
                Matrix4x4 viewProjectionMatrix = mainCamera.projectionMatrix * mainCamera.worldToCameraMatrix;
                cgContext.cmd.SetComputeMatrixParam(data.Cs, ViewProjectionMatrixId, viewProjectionMatrix);
                
                int mainKernel = data.Cs.FindKernel("CSMain");
                cgContext.cmd.SetComputeBufferParam(data.Cs, mainKernel, ShapeBufferId, data.ShapeBufferHandle);
                cgContext.cmd.SetComputeBufferParam(data.Cs, mainKernel, NodeBufferId, data.NodeBufferHandle);
                cgContext.cmd.SetComputeTextureParam(data.Cs, mainKernel, "_ResultTexture", data.ResultTextureHandle);
                
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
        
        private static readonly int ShapeBufferId = Shader.PropertyToID("_ShapeBuffer");
        private static readonly int NodeBufferId = Shader.PropertyToID("_NodeBuffer");

        public ComputeRaymarchManager RaymarchManager; // needs change...
        
        [SerializeField] private ComputeShader raymarchCs;
        [SerializeField] private RaymarchSetting Setting = new();

        private RaymarchComputePass computePass;

        public override void Create()
        {
            if (raymarchCs == null) return;

            if (computePass == null)
            {
                computePass = new RaymarchComputePass(raymarchCs);
                computePass.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
            }
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
                if (RaymarchManager == null) return;
                
                computePass.SetRaymarchSettings(Setting);
                computePass.SetupShapeResultBuffer(RaymarchManager.ShapeData);
                computePass.SetupNodeBuffer(RaymarchManager.NodeData);
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
    }
}
