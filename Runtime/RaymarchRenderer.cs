using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rayman
{
#if UNITY_EDITOR
    public enum DebugModes { None, Color, Normal, Hitmap, BoundingVolume, }
#endif
    [ExecuteInEditMode]
    public class RaymarchRenderer : MonoBehaviour, IRaymarchRendererControl
    {
        public static readonly int ShapeBufferId = Shader.PropertyToID("_ShapeBuffer");
        public static readonly int DistortionCountId = Shader.PropertyToID("_DistortionCount");
        public static readonly int DistortionBufferId = Shader.PropertyToID("_DistortionBuffer");
        public static readonly int NodeBufferId = Shader.PropertyToID("_NodeBuffer");
        public static readonly int MaxStepsId = Shader.PropertyToID("_MaxSteps");
        public static readonly int MaxDistanceId = Shader.PropertyToID("_MaxDistance");
        public static readonly int ShadowMaxStepsId = Shader.PropertyToID("_ShadowMaxSteps");
        public static readonly int ShadowMaxDistanceId = Shader.PropertyToID("_ShadowMaxDistance");
        public static readonly int ShadowBiasId = Shader.PropertyToID("_ShadowBiasVal");
        public static readonly int DebugModeId = Shader.PropertyToID("_DebugMode");
        public static readonly int BoundsDisplayThresholdId = Shader.PropertyToID("_BoundsDisplayThreshold");

        [SerializeField] protected Renderer mainRenderer;
        [SerializeField] protected Shader mainShader;
        [SerializeField] protected List<RaymarchShape> shapes = new();
        [SerializeField] protected bool buildOnAwake;
        [SerializeField] protected int maxSteps = 64;
        [SerializeField] protected float maxDistance = 100f;
        [SerializeField] protected int shadowMaxSteps = 32;
        [SerializeField] protected float shadowMaxDistance = 30f;
        [SerializeField] protected float shadowBias = 0.013f;
#if UNITY_EDITOR
        [Header("Debugging")] [SerializeField] protected Shader debugShader;
        [SerializeField] protected DebugModes debugMode = DebugModes.None;
        [SerializeField] protected bool drawGizmos;
        [SerializeField] protected bool showLabel;
        [SerializeField] protected int boundsDisplayThreshold = 300;
#endif
        protected Material matInstance;
        protected ISpatialStructure<AABB> bvh;
        protected BoundingVolume<AABB>[] boundingVolumes;
        protected ShapeData[] shapeData;
        protected DistortionData[] distortionData;
        protected NodeData<AABB>[] nodeData;
        protected GraphicsBuffer shapeBuffer;
        protected GraphicsBuffer distortionBuffer;
        protected GraphicsBuffer nodeBuffer;
        
        protected virtual void Awake()
        {
            if (buildOnAwake)
                Build();
        }

        protected virtual void LateUpdate()
        {
            if (boundingVolumes == null) return;
            
            RaymarchUtils.SyncBoundingVolumes(ref bvh, ref boundingVolumes);
            RaymarchUtils.UpdateShapeData(boundingVolumes, ref shapeData);
            RaymarchUtils.UpdateOperationData(boundingVolumes, ref distortionData);
            RaymarchUtils.FillNodeData(bvh, ref nodeData);
            
            shapeBuffer?.SetData(shapeData);
            distortionBuffer?.SetData(distortionData);
            nodeBuffer?.SetData(nodeData);
        }
        
        public virtual void AddShape(RaymarchShape shape)
        {
            if (shapes.Contains(shape)) return;
            
            shapes.Add(shape);
        }

        public virtual void RemoveShape(RaymarchShape shape)
        {
            if (!shapes.Contains(shape)) return;
            
            int i = shapes.FindIndex(b => b == shape);
            shapes.RemoveAt(i);
        }
        
        [ContextMenu("Build")]
        public void Build()
        {
#if UNITY_EDITOR
            SetupMaterialInEditor();
#endif
            if (matInstance == null)
            {
                if (mainShader == null) return;
                matInstance = CoreUtils.CreateEngineMaterial(mainShader);
            }
            mainRenderer.material = matInstance;
            
            int shapeCount = shapes.Count(s => s.gameObject.activeInHierarchy);
            shapeData = new ShapeData[shapeCount];
            SetupShapeBuffer(shapeCount, ref matInstance, ref shapeBuffer);
            
            int distortionCount = shapes.Count(s => s.Settings.Distortion.Enabled && s.gameObject.activeInHierarchy);
            distortionData = new DistortionData[distortionCount];
            SetupDistortionBuffer(distortionCount, ref matInstance, ref distortionBuffer);

            boundingVolumes = RaymarchUtils.CreateBoundingVolumes<AABB>(shapes).ToArray();
            bvh = RaymarchUtils.CreateSpatialStructure(boundingVolumes);
#if UNITY_EDITOR
            SpatialStructureDebugger.Add(bvh);
#endif
            
            int nodesCount = SpatialNode<AABB>.GetNodesCount(bvh.Root);
            nodeData = new NodeData<AABB>[nodesCount];
            SetupNodeBuffer(nodesCount, ref matInstance, ref nodeBuffer);
            
            SetupRaymarchProperties(ref matInstance);

#if UNITY_EDITOR
            void SetupMaterialInEditor()
            {
                if (matInstance != null) return;
                if (debugMode != DebugModes.None)
                {
                    if (debugShader == null) return;
                    matInstance = CoreUtils.CreateEngineMaterial(debugShader);
                    SetupDebugProperties(ref matInstance);
                }
            }
#endif
        }
        
        protected void SetupShapeBuffer(int count, ref Material mat, ref GraphicsBuffer buffer)
        {
            if (count == 0) return;
            
            buffer?.Release();
            buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, count, Marshal.SizeOf(typeof(ShapeData)));
            mat.SetBuffer(ShapeBufferId, buffer);
        }

        protected void SetupDistortionBuffer(int count, ref Material mat, ref GraphicsBuffer buffer)
        {
            if (count == 0) return;
            
            buffer?.Release();
            buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, count, Marshal.SizeOf(typeof(DistortionData)));
            mat.SetInt(DistortionCountId, count);
            mat.SetBuffer(DistortionBufferId, buffer);
            mat.EnableKeyword("_DISTORTION_FEATURE");
        }

        protected void SetupNodeBuffer(int count, ref Material mat, ref GraphicsBuffer buffer)
        {
            if (count == 0) return;
            
            buffer?.Release();
            buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, count, Marshal.SizeOf(typeof(NodeData<AABB>)));
            mat.SetBuffer(NodeBufferId, buffer);
        }
        
        protected void SetupRaymarchProperties(ref Material mat)
        {
            mat.SetInt(MaxStepsId, maxSteps);
            mat.SetFloat(MaxDistanceId, maxDistance);
            mat.SetInt(ShadowMaxStepsId, shadowMaxSteps);
            mat.SetFloat(ShadowMaxDistanceId, shadowMaxDistance);
            mat.SetFloat(ShadowBiasId, shadowBias);
        }
        
#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            if (mainRenderer == null)
                mainRenderer = GetComponent<Renderer>();
            
            if (matInstance == null) return;
            
            bool rebuild = matInstance.shader != (debugMode != DebugModes.None ? debugShader : mainShader);
            if (rebuild)
            {
                DestroyImmediate(matInstance);
                matInstance = null;
                Build();
                return;
            }
            
            SetupRaymarchProperties(ref matInstance);
            if (debugMode != DebugModes.None)
                SetupDebugProperties(ref matInstance);
        }
        
        protected virtual void OnDrawGizmos()
        {
            if (bvh != null && drawGizmos)
                bvh.DrawStructure(showLabel);
        }

        protected virtual void OnGUI()
        {
            if (boundingVolumes == null)
                Build();
            
            if (boundingVolumes == null) return;
            
            RaymarchUtils.SyncBoundingVolumes(ref bvh, ref boundingVolumes);
            RaymarchUtils.UpdateShapeData(boundingVolumes, ref shapeData);
            RaymarchUtils.UpdateOperationData(boundingVolumes, ref distortionData);
        }
        
        protected void SetupDebugProperties(ref Material mat)
        {
            mat.SetInt(DebugModeId, (int)debugMode);
            mat.SetInt(BoundsDisplayThresholdId, boundsDisplayThreshold);
        }

        [ContextMenu("Find All Shapes")]
        private void FindAllShapes()
        {
            shapes = RaymarchUtils.GetChildrenByHierarchical<RaymarchShape>(transform);
        }
#endif
    }
    
    public class BoundingVolume<T> where T : struct, IBounds<T>
    {
        public RaymarchShape Source;
        public T Bounds;

        public BoundingVolume(RaymarchShape shape, T bounds)
        {
            Source = shape;
            Bounds = bounds;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct ShapeData
    {
        public Matrix4x4 Transform;
        public int Type;
        public Vector3 Size;
        public float Roundness;
        public int Operation;
        public float Smoothness;
        public Vector4 Color;
        public Vector4 EmissionColor;
        public float EmissionIntensity;
        public int DistortionEnabled;

        public ShapeData(Transform sourceTransform, RaymarchShape.Setting setting)
        {
            Transform = setting.UseLossyScale ? sourceTransform.worldToLocalMatrix : 
                Matrix4x4.TRS(sourceTransform.position, sourceTransform.rotation, Vector3.one).inverse;
            Type = (int)setting.Shape;
            Size = setting.Size;
            Roundness = setting.Roundness;
            Operation = (int)setting.Operation;
            Smoothness = setting.Smoothness;
            Color = setting.Color;
            EmissionColor = setting.EmissionColor;
            EmissionIntensity = setting.EmissionIntensity;
            DistortionEnabled = setting.Distortion.Enabled ? 1 : 0;
        }
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct DistortionData
    {
        public int Id;
        public int Type;
        public float Amount;

        public DistortionData(int id, int type, float amount)
        {
            Id = id;
            Type = type;
            Amount = amount;
        }
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct NodeData<T> where T : struct, IBounds<T>
    {
        public int Id;
        public T Bounds;
        public int Parent;
        public int Left;
        public int Right;
    }
}