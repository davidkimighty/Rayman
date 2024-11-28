#define RAYMARCH_DEBUG_ENABLED

using System;
using System.Collections.Generic;
using System.Linq;
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
            public int RaymarchMaxSteps = 128;
            public int RaymarchMaxDistance = 100;
        }
        
        public class RaymarchComputePass : ScriptableRenderPass
        {
            private class PassData
            {
                public ComputeShader Cs;
                public Setting Setting;
                public BufferHandle ShapeBufferHandle;
                public BufferHandle ResultBufferHandle;
#if RAYMARCH_DEBUG_ENABLED
                public TextureHandle ResultTextureHandle;
#endif
            }
            
            private ComputeShader cs;
            private Setting setting;
            private GraphicsBuffer shapeBuffer;
            private GraphicsBuffer resultBuffer;
            private ComputeShapeData[] shapeData;
            private ComputeResultData[] resultData;
            
            private List<ComputeRaymarchRenderer> renderers = new();
            private int prevCount = -1;
            private bool needRefresh = true;

            public RaymarchComputePass(ComputeShader cs, Setting setting)
            {
                this.cs = cs;
                this.setting = setting;
            }

            public void AddRenderer(ComputeRaymarchRenderer renderer)
            {
                if (renderers.Contains(renderer) || renderer.Shapes.Count == 0) return;
                
                renderers.Add(renderer);
                needRefresh = true;
            }
            
            public void RemoveRenderer(ComputeRaymarchRenderer renderer)
            {
                if (!renderers.Contains(renderer)) return;
                
                renderers.Remove(renderer);
                needRefresh = true;
            }
            
            public void Setup()
            {
                int count = renderers.Sum(r => r.Shapes.Count);
                if (count == 0) return;
                
                SetupBuffers(count);
                UpdateData();
#if RAYMARCH_DEBUG_ENABLED
                requiresIntermediateTexture = true;
#endif
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                if (shapeBuffer == null || resultBuffer == null) return;
                
                BufferHandle shapeBufferHandle = renderGraph.ImportBuffer(shapeBuffer);
                BufferHandle resultBufferHandle = renderGraph.ImportBuffer(resultBuffer);
#if RAYMARCH_DEBUG_ENABLED
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                TextureHandle source = resourceData.activeColorTexture;
                TextureDesc destinationDesc = renderGraph.GetTextureDesc(source);
                destinationDesc.name = "Raymarch Result Texture";
                destinationDesc.clearBuffer = false;
                destinationDesc.enableRandomWrite = true;
                TextureHandle destination = renderGraph.CreateTexture(destinationDesc);
#endif
                using (var builder = renderGraph.AddComputePass("Raymarch ComputePass", out PassData passData))
                {
                    passData.Cs = cs;
                    passData.Setting = setting;
                    passData.ShapeBufferHandle = shapeBufferHandle;
                    passData.ResultBufferHandle = resultBufferHandle;
                    
                    builder.UseBuffer(passData.ShapeBufferHandle);
                    builder.UseBuffer(passData.ResultBufferHandle, AccessFlags.Write);
#if RAYMARCH_DEBUG_ENABLED
                    passData.ResultTextureHandle = destination;
                    builder.UseTexture(passData.ResultTextureHandle, AccessFlags.Write);
#endif
                    builder.SetRenderFunc((PassData data, ComputeGraphContext cgContext) => ExecutePass(data, cgContext));
                }
                
                //resultBuffer.GetData(resultData);
#if RAYMARCH_DEBUG_ENABLED
                resourceData.cameraColor = destination;
#endif
            }
            
            private static void ExecutePass(PassData data, ComputeGraphContext cgContext)
            {
                int mainKernel = data.Cs.FindKernel("CSMain");
                cgContext.cmd.SetComputeFloatParam(data.Cs, "screenWidth", Camera.main.pixelWidth);
                cgContext.cmd.SetComputeFloatParam(data.Cs, "screenHeight", Camera.main.pixelHeight);
                cgContext.cmd.SetComputeVectorParam(data.Cs, "cameraPosition", Camera.main.transform.position);
                cgContext.cmd.SetComputeMatrixParam(data.Cs, "cameraToWorld", Camera.main.cameraToWorldMatrix);
                cgContext.cmd.SetComputeMatrixParam(data.Cs, "inverseProjectionMatrix", Camera.main.projectionMatrix.inverse);
                cgContext.cmd.SetComputeMatrixParam(data.Cs, "inverseProjectionMatrix", Camera.main.projectionMatrix.inverse);
                Matrix4x4 viewProjectionMatrix = Camera.main.projectionMatrix * Camera.main.worldToCameraMatrix;
                cgContext.cmd.SetComputeMatrixParam(data.Cs, "viewProjectionMatrix", viewProjectionMatrix);
                
                cgContext.cmd.SetComputeIntParam(data.Cs, "raymarchMaxSteps", data.Setting.RaymarchMaxSteps);
                cgContext.cmd.SetComputeIntParam(data.Cs, "raymarchMaxDistance", data.Setting.RaymarchMaxDistance);
                
                cgContext.cmd.SetComputeBufferParam(data.Cs, mainKernel, ShapeBufferId, data.ShapeBufferHandle);
                cgContext.cmd.SetComputeBufferParam(data.Cs, mainKernel, ResultBufferId, data.ResultBufferHandle);
#if RAYMARCH_DEBUG_ENABLED
                cgContext.cmd.SetComputeTextureParam(data.Cs, mainKernel, "resultTexture", data.ResultTextureHandle);
#endif
                int threadGroupsX = Mathf.CeilToInt(Camera.main.pixelWidth / 16.0f);
                int threadGroupsY = Mathf.CeilToInt(Camera.main.pixelHeight / 16.0f);
                cgContext.cmd.DispatchCompute(data.Cs, mainKernel, threadGroupsX, threadGroupsY, 1);
            }

            private void SetupBuffers(int count)
            {
                if (prevCount == count || !needRefresh) return;
                
                shapeBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, count, ComputeShapeData.Stride);
                shapeData = new ComputeShapeData[count];
                
                int totalThreads = Camera.main.pixelWidth * Camera.main.pixelHeight;
                resultBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, totalThreads, ComputeResultData.Stride);
                resultData = new ComputeResultData[totalThreads];
                resultBuffer.SetData(resultData);
                Shader.SetGlobalBuffer(ResultBufferId, resultBuffer);

                cs.SetInt(ShapeCountId, count);
                prevCount = count;
                needRefresh = false;
            }

            private void UpdateData()
            {
                if (shapeBuffer == null) return;
                
                for (int i = 0; i < renderers.Count; i++)
                {
                    ComputeRaymarchRenderer renderer = renderers[i];
                    for (int j = 0; j < renderer.Shapes.Count; j++)
                    {
                        Transform trans = renderer.Shapes[j].transform;
                        RaymarchShape.Setting settings = renderer.Shapes[j].ShapeSetting;
                        shapeData[i] = new ComputeShapeData
                        {
                            GroupId = i, Id = j,
                            Transform = Matrix4x4.Inverse(Matrix4x4.TRS(trans.position, trans.rotation, trans.lossyScale)),
                            Type = (int)settings.Type,
                            Size = settings.Size,
                            Roundness = settings.Roundness,
                            Combination = (int)settings.Combination,
                            Smoothness = settings.Roundness,
                        };
                    }
                }
                shapeBuffer.SetData(shapeData);
            }
        }

        private static readonly int ShapeCountId = Shader.PropertyToID("shapeCount");
        private static readonly int ShapeBufferId = Shader.PropertyToID("shapeBuffer");
        private static readonly int ResultBufferId = Shader.PropertyToID("resultBuffer");
        
        [SerializeField] private ComputeShader computeShader;
        [SerializeField] private Setting setting;
        
        private RaymarchComputePass computePass;
        
        public RaymarchComputePass ComputePass => computePass;

        /// <inheritdoc/>
        public override void Create()
        {
            computePass = new RaymarchComputePass(computeShader, setting);
            computePass.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (!SystemInfo.supportsComputeShaders)
            {
                Debug.LogWarning("Device does not support compute shaders.");
                return;
            }
            if (computeShader == null) return;

            if (renderingData.cameraData.camera == Camera.main)
            {
                computePass.Setup();
                renderer.EnqueuePass(computePass);
            }
        }
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct ComputeShapeData
    {
        public const int Stride = sizeof(float) * 21 + sizeof(int) * 4;

        public int GroupId;
        public int Id;
        public Matrix4x4 Transform;
        public int Type;
        public Vector3 Size;
        public float Roundness;
        public int Combination;
        public float Smoothness;
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct ComputeResultData
    {
        public const int Stride = sizeof(float) * 4;

        public Vector3 HitPoint;
        public float TravelDistance;
    }
}
