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
    public class ComputeRaymarchFeature : ScriptableRendererFeature
    {
        public class RaymarchComputePass : ScriptableRenderPass
        {
            private class PassData
            {
                public ComputeShader Cs;
                public BufferHandle ShapeBufferHandle;
                public BufferHandle NodeBufferHandle;
                public BufferHandle RootNodeIndicesBufferHandle;
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
            private GraphicsBuffer rootNodeIndicesBuffer;
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
                BufferHandle rootNodeIndicesBufferHandle = renderGraph.ImportBuffer(rootNodeIndicesBuffer);
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
                    passData.RootNodeIndicesBufferHandle = rootNodeIndicesBufferHandle;
                    passData.ResultBufferHandle = resultBufferHandle;
                    builder.UseBuffer(passData.ShapeBufferHandle);
                    builder.UseBuffer(passData.NodeBufferHandle);
                    builder.UseBuffer(passData.RootNodeIndicesBufferHandle);
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
                cs.SetInt(RaymarchRenderer.MaxDistanceId, setting.MaxDistance);
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
                int shapeCount = shapeData.Length;
                if (shapeBuffer == null || shapeBuffer.count != shapeCount)
                {
                    shapeBuffer?.Release();
                    shapeBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, shapeCount,
                        Marshal.SizeOf(typeof(ShapeData)));
                    
                    int totalThreads = Screen.width * Screen.height;
                    resultBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, totalThreads,
                        Marshal.SizeOf(typeof(ComputeResultData)));
                    // resultData = new ComputeResultData[totalThreads];
                    Shader.SetGlobalBuffer(ResultBufferId, resultBuffer);
                    Debug.Log($"[Raymarch Feature] ShapeBuffer[{shapeCount}] & ResultBuffer[{totalThreads}] initialized.");
                }
                shapeBuffer.SetData(shapeData);
            }

            public void SetupNodeBuffer(NodeData<AABB>[] nodeData, int[] rootNodeIndices)
            {
                int nodeCount = nodeData.Length;
                if (nodeBuffer == null || nodeBuffer.count != nodeCount)
                {
                    nodeBuffer?.Release();
                    nodeBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, nodeCount,
                        Marshal.SizeOf(typeof(NodeData<AABB>)));
                    Debug.Log($"[Raymarch Feature] NodeBuffer[{nodeCount}] initialized.");
                }
                nodeBuffer.SetData(nodeData);

                int rootNodeCount = rootNodeIndices.Length;
                if (rootNodeIndicesBuffer == null || rootNodeIndicesBuffer.count != rootNodeCount)
                {
                    cs.SetInt(GroupCountId, rootNodeCount);
                    rootNodeIndicesBuffer?.Release();
                    rootNodeIndicesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, rootNodeCount, sizeof(int));
                    Debug.Log($"[Raymarch Feature] RootNodeIndicesBuffer[{rootNodeCount}] initialized.");
                }
                rootNodeIndicesBuffer.SetData(rootNodeIndices);
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
                cgContext.cmd.SetComputeBufferParam(data.Cs, mainKernel, RootNodeIndicesBufferId, data.RootNodeIndicesBufferHandle);
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
        private static readonly int GroupCountId = Shader.PropertyToID("_GroupCount");
        private static readonly int RootNodeIndicesBufferId = Shader.PropertyToID("_RootNodeIndices");
        private static readonly int ResultBufferId = Shader.PropertyToID("_ResultBuffer");

        private static List<RaymarchRenderer> RaymarchRenderers = new();
        
        public RaymarchSetting Setting;
        
        [SerializeField] private ComputeShader raymarchCs;
#if UNITY_EDITOR
        [SerializeField] private ComputeShader debugCs;
#endif
        private RaymarchComputePass computePass;
        private ShapeData[] combinedShapeData;
        private DistortionData[] combinedDistortionData;
        private NodeData<AABB>[] combinedNodeData;
        private int[] rootNodeIndices;

        public static void AddRenderer(RaymarchRenderer raymarchRenderer)
        {
            if (RaymarchRenderers.Contains(raymarchRenderer)) return;
            RaymarchRenderers.Add(raymarchRenderer);
        }
        
        public static void RemoveRenderer(RaymarchRenderer raymarchRenderer)
        {
            if (!RaymarchRenderers.Contains(raymarchRenderer)) return;
            RaymarchRenderers.Remove(raymarchRenderer);
        }
        
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

            RaymarchRenderers = FindObjectsByType<RaymarchRenderer>(FindObjectsSortMode.None).ToList();
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
                if (!CombinePassData()) return;
                
                computePass.InitializePassSettings();
                computePass.SetupShapeResultBuffer(combinedShapeData);
                computePass.SetupNodeBuffer(combinedNodeData, rootNodeIndices);
                renderer.EnqueuePass(computePass);
            }
        }

        private bool CombinePassData()
        {
            int shapeLength = RaymarchRenderers.Sum(r => r.ShapeData?.Length ?? 0);
            int distortionLength = RaymarchRenderers.Sum(r => r.DistortionData?.Length ?? 0);
            int nodeLength = RaymarchRenderers.Sum(r => r.NodeData?.Length ?? 0);
            
            combinedShapeData = EnsureArraySize(combinedShapeData, shapeLength);
            combinedDistortionData = EnsureArraySize(combinedDistortionData, distortionLength);
            combinedNodeData = EnsureArraySize(combinedNodeData, nodeLength);

            int shapeOffset = 0, distortionOffset = 0, nodeOffset = 0, nodeIndexOffset = 0;
            rootNodeIndices = new int[RaymarchRenderers.Count];
            
            for (int i = 0; i < RaymarchRenderers.Count; i++)
            {
                RaymarchRenderer raymarchRenderer = RaymarchRenderers[i];
                ProcessDistortionData(raymarchRenderer);
                ProcessNodeData(raymarchRenderer, i);
                ProcessShapeData(raymarchRenderer);
            }
            return shapeLength != 0 && nodeLength != 0;

            void ProcessDistortionData(RaymarchRenderer raymarchRenderer)
            {
                if (raymarchRenderer.DistortionData == null) return;
                
                for (int j = 0; j < raymarchRenderer.DistortionData.Length; j++)
                {
                    raymarchRenderer.DistortionData[j].Id += shapeOffset;
                    combinedDistortionData[distortionOffset++] = raymarchRenderer.DistortionData[j];
                }
            }

            void ProcessNodeData(RaymarchRenderer raymarchRenderer, int groupIndex)
            {
                if (raymarchRenderer.NodeData == null) return;
                
                for (int j = 0; j < raymarchRenderer.NodeData.Length; j++)
                {
                    if (raymarchRenderer.NodeData[j].Id == -1)
                    {
                        raymarchRenderer.NodeData[j].Left += nodeIndexOffset;
                        raymarchRenderer.NodeData[j].Right += nodeIndexOffset;
                    }
                    else
                        raymarchRenderer.NodeData[j].Id += shapeOffset;
                    combinedNodeData[nodeOffset++] = raymarchRenderer.NodeData[j];
                }
                rootNodeIndices[groupIndex] = nodeIndexOffset;
                nodeIndexOffset += raymarchRenderer.NodeData.Length;
            }

            void ProcessShapeData(RaymarchRenderer raymarchRenderer)
            {
                if (raymarchRenderer.ShapeData == null) return;
                
                Array.Copy(raymarchRenderer.ShapeData, 0, combinedShapeData, shapeOffset, raymarchRenderer.ShapeData.Length);
                shapeOffset += raymarchRenderer.ShapeData.Length;
            }
            
            static T[] EnsureArraySize<T>(T[] array, int size)
            {
                if (array == null)
                    return new T[size];
                if (array.Length != size)
                    Array.Resize(ref array, size);
                return array;
            }
        }
    }
    
    public struct Ray
    {
        public Vector3 Origin;
        public Vector3 Direction;
        public float MaxDistance;

        public Ray(Vector3 origin, Vector3 direction, float maxDistance)
        {
            Origin = origin;
            Direction = direction;
            MaxDistance = maxDistance;
        }

        public float Intersection(AABB aabb)
        {
            Vector3 invDir = new Vector3(1f / Direction.x, 1f / Direction.y, 1f / Direction.z);
            Vector3 tMin = Vector3.Scale(aabb.Min - Origin, invDir);
            Vector3 tMax = Vector3.Scale(aabb.Max - Origin, invDir);
            Vector3 t1 = Vector3.Min(tMin, tMax);
            Vector3 t2 = Vector3.Max(tMin, tMax);
            float dstFar = Mathf.Min(t2.x, Mathf.Min(t2.y, t2.z));
            float dstNear = Mathf.Max(t1.x, Mathf.Max(t1.y, t1.z));
            return dstNear > 0 ? dstNear : MaxDistance;
        }
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
    
    [Serializable]
    public class RaymarchSetting
    {
        public int MaxSteps = 64;
        public int MaxDistance = 100;
#if UNITY_EDITOR
        public DebugModes DebugMode = 0;
        public int BoundsDisplayThreshold = 50;
#endif
        private bool initializeSettings;

        public bool TriggerState => initializeSettings;

        public void SetTrigger(bool state = true) => initializeSettings = state;
    }
}
