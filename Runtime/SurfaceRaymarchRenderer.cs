using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rayman
{
    public class SurfaceRaymarchRenderer : RaymarchRenderer
    {
        [Header("Surface Raymarch")]
        [SerializeField] private int maxSteps = 64;
        [SerializeField] private float maxDistance = 100f;
        [SerializeField] private int shadowMaxSteps = 32;
        [SerializeField] private float shadowMaxDistance = 30f;
        
        private Material mat;
        private ISpatialStructure<AABB> bvh;
        private BoundingVolume<AABB>[] boundingVolumes;
        private ShapeData[] shapeData;
        private DistortionData[] distortionData;
        private NodeData<AABB>[] nodeData;
        private GraphicsBuffer shapeBuffer;
        private GraphicsBuffer distortionBuffer;
        private GraphicsBuffer nodeBuffer;
        
        private void Awake()
        {
            Build();
        }
        
        private void Update()
        {
            if (boundingVolumes == null) return;
            
            SyncBoundingVolumes<AABB>(ref bvh, ref boundingVolumes);
            UpdateShapeData<AABB>(boundingVolumes, ref shapeData);
            UpdateOperationData<AABB>(boundingVolumes, ref distortionData);
            FillNodeData<AABB>(bvh, ref nodeData);
            
            shapeBuffer?.SetData(shapeData);
            distortionBuffer?.SetData(distortionData);
            nodeBuffer?.SetData(nodeData);
        }
        
        [ContextMenu("Build")]
        public void Build()
        {
#if UNITY_EDITOR
            SetupMaterialInEditor();
#endif
            if (mat == null)
            {
                if (mainShader == null) return;
                mat = CoreUtils.CreateEngineMaterial(mainShader);
            }
            mainRenderer.material = mat;
            
            int shapeCount = shapes.Count(s => s.gameObject.activeInHierarchy);
            shapeData = new ShapeData[shapeCount];
            SetupShapeBuffer(shapeCount, ref mat, ref shapeBuffer);
            
            int distortionCount = shapes.Count(s => s.Settings.Distortion.Enabled && s.gameObject.activeInHierarchy);
            distortionData = new DistortionData[distortionCount];
            SetupDistortionBuffer(distortionCount, ref mat, ref distortionBuffer);

            boundingVolumes = CreateBoundingVolumes<AABB>().ToArray();
            bvh = CreateSpatialStructure<AABB>(boundingVolumes);
            
            int nodesCount = SpatialNode<AABB>.GetNodesCount(bvh.Root);
            nodeData = new NodeData<AABB>[nodesCount];
            SetupNodeBuffer(nodesCount, ref mat, ref nodeBuffer);
            
            SetupRaymarchProperties(ref mat);

#if UNITY_EDITOR
            void SetupMaterialInEditor()
            {
                if (mat != null) return;
                if (debugMode != DebugModes.None)
                {
                    if (debugShader == null) return;
                    mat = CoreUtils.CreateEngineMaterial(debugShader);
                    SetupDebugProperties(ref mat);
                }
            }
#endif
        }

        private void SetupShapeBuffer(int count, ref Material mat, ref GraphicsBuffer shapeBuffer)
        {
            if (count == 0) return;
            
            shapeBuffer?.Release();
            shapeBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, count, Marshal.SizeOf(typeof(ShapeData)));
            mat.SetBuffer(ShapeBufferId, shapeBuffer);
        }

        private void SetupDistortionBuffer(int count, ref Material mat, ref GraphicsBuffer distortionBuffer)
        {
            if (count == 0) return;
            
            distortionBuffer?.Release();
            distortionBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, count, Marshal.SizeOf(typeof(DistortionData)));
            mat.SetInt(DistortionCountId, count);
            mat.SetBuffer(DistortionBufferId, distortionBuffer);
            mat.EnableKeyword("_DISTORTION_FEATURE");
        }

        private void SetupNodeBuffer(int count, ref Material mat, ref GraphicsBuffer nodeBuffer)
        {
            if (count == 0) return;
            
            nodeBuffer?.Release();
            nodeBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, count, Marshal.SizeOf(typeof(NodeData<AABB>)));
            mat.SetBuffer(NodeBufferId, nodeBuffer);
        }
        
        private void SetupRaymarchProperties(ref Material mat)
        {
            mat.SetInt(MaxStepsId, maxSteps);
            mat.SetFloat(MaxDistanceId, maxDistance);
            mat.SetInt(ShadowMaxStepsId, shadowMaxSteps);
            mat.SetFloat(ShadowMaxDistanceId, shadowMaxDistance);
            mat.SetFloat(ShadowBiasId, shadowBias);
        }
        
#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            
            if (mat == null) return;
            
            bool rebuild = mat.shader != (debugMode != DebugModes.None ? debugShader : mainShader);
            if (rebuild)
            {
                DestroyImmediate(mat);
                mat = null;
                Build();
                return;
            }
            
            SetupRaymarchProperties(ref mat);
            if (debugMode != DebugModes.None)
                SetupDebugProperties(ref mat);
        }

        private void OnDrawGizmos()
        {
            if (bvh != null && drawGizmos)
                bvh.DrawStructure(showLabel);
        }

        private void OnGUI()
        {
            if (boundingVolumes == null)
            {
                Build();
                SyncBoundingVolumes<AABB>(ref bvh, ref boundingVolumes);
                UpdateShapeData<AABB>(boundingVolumes, ref shapeData);
                UpdateOperationData<AABB>(boundingVolumes, ref distortionData);
            }
        }
        
        private void SetupDebugProperties(ref Material mat)
        {
            mat.SetInt(DebugModeId, (int)debugMode);
            mat.SetInt(BoundsDisplayThresholdId, boundsDisplayThreshold);
        }
#endif
    }
}