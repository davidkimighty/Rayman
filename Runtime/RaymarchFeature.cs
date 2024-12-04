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
        public struct Setting
        {
            [HideInInspector] public bool ReinitSetting;
            [HideInInspector] public bool RebuildBuffers;
            
            public int MaxSteps;
            public int MaxDistance;

            public void SetTriggers(bool state)
            {
                ReinitSetting = state;
                RebuildBuffers = state;
            }
        }
        
        public class RaymarchComputePass : ScriptableRenderPass
        {
            private class PassData
            {
                public ComputeShader Cs;
                public BufferHandle ShapeBufferHandle;
                public BufferHandle ResultBufferHandle;
#if RAYMARCH_DEBUG_ENABLED
                public TextureHandle ResultTextureHandle;
#endif
            }
            
            private ComputeShader cs;
            private GraphicsBuffer shapeBuffer;
            private GraphicsBuffer resultBuffer;
            private ComputeShapeData[] shapeData;
            private ComputeResultData[] resultData;
            private int prevCount = -1;

            public RaymarchComputePass(ComputeShader cs)
            {
                this.cs = cs;
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
                resourceData.cameraColor = destination;
#endif
                using (var builder = renderGraph.AddComputePass("Raymarch ComputePass", out PassData passData))
                {
                    passData.Cs = cs;
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
            }

            public void Setup(List<ComputeRaymarchRenderer> renderers, Setting setting)
            {
                int count = renderers.Sum(r => r.Shapes.Count);
                if (count == 0) return;

                SetupSettings(setting);
                SetupBuffers(count, setting.RebuildBuffers);
                UpdateShapeData(renderers);
#if RAYMARCH_DEBUG_ENABLED
                requiresIntermediateTexture = true;
#endif
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
                cgContext.cmd.SetComputeBufferParam(data.Cs, mainKernel, ResultBufferId, data.ResultBufferHandle);
#if RAYMARCH_DEBUG_ENABLED
                cgContext.cmd.SetComputeTextureParam(data.Cs, mainKernel, "resultTexture", data.ResultTextureHandle);
#endif
                int threadGroupsX = Mathf.CeilToInt(Camera.main.pixelWidth / 8.0f);
                int threadGroupsY = Mathf.CeilToInt(Camera.main.pixelHeight / 8.0f);
                cgContext.cmd.DispatchCompute(data.Cs, mainKernel, threadGroupsX, threadGroupsY, 1);
            }

            private void SetupSettings(Setting setting)
            {
                if (!setting.ReinitSetting) return;
                
                cs.SetInt(ScreenWidthId, Camera.main.pixelWidth);
                cs.SetInt(ScreenHeightId, Camera.main.pixelHeight);
                cs.SetInt(RaymarchMaxStepsId, setting.MaxSteps);
                cs.SetInt(RaymarchMaxDistanceId, setting.MaxDistance);
            }
            
            private void SetupBuffers(int count, bool rebuild)
            {
                if (prevCount == count || !rebuild) return;
                
                shapeBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, count, ComputeShapeData.Stride);
                shapeData = new ComputeShapeData[count];
                
                int totalThreads = Camera.main.pixelWidth * Camera.main.pixelHeight;
                resultBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, totalThreads, ComputeResultData.Stride);
                resultData = new ComputeResultData[totalThreads];
                resultBuffer.SetData(resultData);
                Shader.SetGlobalBuffer(ResultBufferId, resultBuffer);

                cs.SetInt(ShapeCountId, count);
                prevCount = count;
            }

            private void UpdateShapeData(List<ComputeRaymarchRenderer> renderers)
            {
                if (shapeBuffer == null) return;

                int shapeIndex = 0;
                // foreach (RaymarchShape pair in renderers.SelectMany(r => r.Shapes))
                // {
                //     Matrix4x4 transform = pair.transform.worldToLocalMatrix;
                //     Vector3 lossyScale = pair.transform.lossyScale;
                //     RaymarchShape.Setting settings = pair.Settings;
                //     shapeData[shapeIndex] = new ComputeShapeData
                //     {
                //         Transform = transform,
                //         LossyScale = lossyScale,
                //         Type = (int)settings.Type,
                //         Size = settings.Size,
                //         Roundness = settings.Roundness,
                //         Combination = (int)settings.Combination,
                //         Smoothness = settings.Smoothness,
                //         Color = settings.Color,
                //         EmissionColor = settings.EmissionColor,
                //         EmissionIntensity = settings.EmissionIntensity
                //     };
                //     shapeIndex++;
                // }
                for (int i = 0; i < renderers.Count; i++)
                {
                    ComputeRaymarchRenderer renderer = renderers[i];
                    for (int j = 0; j < renderer.Shapes.Count; j++)
                    {
                        if (renderer.Shapes[j] == null) continue;
                        
                        Matrix4x4 transform = renderer.Shapes[j].transform.worldToLocalMatrix;
                        Vector3 lossyScale = renderer.Shapes[j].transform.lossyScale;
                        RaymarchShape.Setting settings = renderer.Shapes[j].Settings;
                        shapeData[shapeIndex] = new ComputeShapeData
                        {
                            GroupId = i,
                            Id = j,
                            Transform = transform,
                            LossyScale = lossyScale,
                            Type = (int)settings.Type,
                            Size = settings.Size,
                            Roundness = settings.Roundness,
                            Combination = (int)settings.Combination,
                            Smoothness = settings.Smoothness,
                            Color = settings.Color,
                            EmissionColor = settings.EmissionColor,
                            EmissionIntensity = settings.EmissionIntensity
                        };
                        shapeIndex++;
                    }
                }
                shapeBuffer.SetData(shapeData);
            }
        }

        public const string DebugKeyword = "RAYMARCH_DEBUG_ENABLED";
        
        private static readonly int ShapeCountId = Shader.PropertyToID("shapeCount");
        private static readonly int ShapeBufferId = Shader.PropertyToID("shapeBuffer");
        private static readonly int ResultBufferId = Shader.PropertyToID("resultBuffer");
        
        private static readonly int ScreenWidthId = Shader.PropertyToID("screenWidth");
        private static readonly int ScreenHeightId = Shader.PropertyToID("screenHeight");
        private static readonly int RaymarchMaxStepsId = Shader.PropertyToID("maxSteps");
        private static readonly int RaymarchMaxDistanceId = Shader.PropertyToID("maxDistance");
        
        [SerializeField] private ComputeShader computeShader;
        [SerializeField] private Setting setting;
        [SerializeField] private List<ComputeRaymarchRenderer> renderers = new();
        
        private RaymarchComputePass computePass;
        
        public override void Create()
        {
            if (computeShader == null) return;
            
            setting.SetTriggers(true);
            
            computePass = new RaymarchComputePass(computeShader);
#if RAYMARCH_DEBUG_ENABLED
            computePass.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
            computeShader.EnableKeyword(DebugKeyword);
#else
            computePass.renderPassEvent = RenderPassEvent.BeforeRenderingPrePasses;
            computeShader.DisableKeyword(DebugKeyword);
#endif
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
                computePass.Setup(renderers, setting);
                setting.SetTriggers(false);
                
                renderer.EnqueuePass(computePass);
            }
        }
        
        public void AddRenderer(ComputeRaymarchRenderer renderer)
        {
            if (renderers.Contains(renderer) || renderer.Shapes.Count == 0) return;
                
            renderers.Add(renderer);
            setting.RebuildBuffers = true;
        }
            
        public void RemoveRenderer(ComputeRaymarchRenderer renderer)
        {
            if (!renderers.Contains(renderer)) return;
                
            renderers.Remove(renderer);
            setting.RebuildBuffers = true;
        }

        public void ClearAndRegister(List<ComputeRaymarchRenderer> renderers)
        {
            this.renderers.Clear();
            this.renderers.AddRange(renderers);
        }
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
