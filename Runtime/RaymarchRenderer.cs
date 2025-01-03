using System;
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
        [Serializable]
        public class Group
        {
            public Material MatRef;
            public List<RaymarchShape> Shapes = new();
        }
        
        public static readonly int ShapeBufferId = Shader.PropertyToID("_ShapeBuffer");
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
        [SerializeField] protected int maxSteps = 64;
        [SerializeField] protected float maxDistance = 100f;
        [SerializeField] protected int shadowMaxSteps = 32;
        [SerializeField] protected float shadowMaxDistance = 30f;
        [SerializeField] protected float shadowBias = 0.006f;
        [SerializeField] protected List<Group> groups = new();
#if UNITY_EDITOR
        [Header("Debugging")]
        [SerializeField] protected bool executeInEditor;
        [SerializeField] protected Shader debugShader;
        [SerializeField] protected DebugModes debugMode = DebugModes.None;
        [SerializeField] protected bool drawGizmos;
        [SerializeField] protected bool showLabel;
        [SerializeField] protected int boundsDisplayThreshold = 300;
#endif
        protected Material matInstance;
        protected ISpatialStructure<AABB> bvh;
        protected BoundingVolume<AABB>[] boundingVolumes;
        protected ShapeData[] shapeData;
        protected NodeData<AABB>[] nodeData;
        protected GraphicsBuffer shapeBuffer;
        protected GraphicsBuffer nodeBuffer;

        public bool IsInitialized => boundingVolumes != null || bvh != null;
        public BoundingVolume<AABB>[] BoundingVolumes => boundingVolumes;

        protected virtual void OnEnable()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying && !executeInEditor) return;
#endif
            if (Build())
            {
                RaymarchDebugger.Add(this);
                SpatialStructureDebugger.Add(bvh);
            }
        }
        
        protected virtual void LateUpdate()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying && !executeInEditor) return;
#endif
            if (!IsInitialized || !mainRenderer.isVisible) return;
            
            RaymarchUtils.SyncBoundingVolumes(ref bvh, ref boundingVolumes);
            RaymarchUtils.UpdateShapeData(boundingVolumes, ref shapeData);
            RaymarchUtils.FillNodeData(bvh, ref nodeData);
            
            shapeBuffer?.SetData(shapeData);
            nodeBuffer?.SetData(nodeData);
        }
        
        protected virtual void OnDisable()
        {
            Release();
        }
        
        public virtual void AddShape(RaymarchShape shape, int groupId)
        {
            if (groupId >= groups.Count || groups[groupId].Shapes.Contains(shape)) return;
            
            groups[groupId].Shapes.Add(shape);
        }

        public virtual void RemoveShape(RaymarchShape shape, int groupId)
        {
            if (groupId >= groups.Count || !groups[groupId].Shapes.Contains(shape)) return;
            
            int i = groups[groupId].Shapes.FindIndex(b => b == shape);
            groups[groupId].Shapes.RemoveAt(i);
        }
        
        [ContextMenu("Build")]
        public bool Build()
        {
#if UNITY_EDITOR
            SetupMaterialInEditor();
#endif
            if (mainShader == null) return false;
            
            if (matInstance == null)
                matInstance = CoreUtils.CreateEngineMaterial(mainShader);
            mainRenderer.material = matInstance;

            List<RaymarchShape> activeShapes = shapes.Where(s => s != null && s.gameObject.activeInHierarchy).ToList();
            int shapeCount = activeShapes.Count;
            if (shapeCount == 0) return false;
            
            boundingVolumes = RaymarchUtils.CreateBoundingVolumes<AABB>(activeShapes)?.ToArray();
            bvh = RaymarchUtils.CreateSpatialStructure(boundingVolumes);
            
            shapeData = new ShapeData[shapeCount];
            SetupShapeBuffer(shapeCount, ref matInstance, ref shapeBuffer);
            
            int nodesCount = SpatialNode<AABB>.GetNodesCount(bvh.Root);
            nodeData = new NodeData<AABB>[nodesCount];
            SetupNodeBuffer(nodesCount, ref matInstance, ref nodeBuffer);
            
            SetupRaymarchProperties(ref matInstance);
            return true;

#if UNITY_EDITOR
            void SetupMaterialInEditor()
            {
                if (debugMode == DebugModes.None || debugShader == null) return;
                
                matInstance = CoreUtils.CreateEngineMaterial(debugShader);
                SetupDebugProperties(ref matInstance);
            }
#endif
        }

        public void Release()
        {
            shapeBuffer?.Release();
            nodeBuffer?.Release();
            
            bvh = null;
            boundingVolumes = null;

            if (matInstance != null)
            {
                if (Application.isPlaying)
                    Destroy(matInstance);
                else
                    DestroyImmediate(matInstance);
                matInstance = null;
            }
            mainRenderer.materials = Array.Empty<Material>();
        }
        
        protected void SetupShapeBuffer(int count, ref Material mat, ref GraphicsBuffer buffer)
        {
            if (count == 0) return;
            
            buffer?.Release();
            buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, count, ShapeData.Stride);
            mat.SetBuffer(ShapeBufferId, buffer);
        }

        protected void SetupNodeBuffer(int count, ref Material mat, ref GraphicsBuffer buffer)
        {
            if (count == 0) return;
            
            buffer?.Release();
            buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, count, NodeData<AABB>.Stride);
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

            if (executeInEditor && !IsInitialized)
            {
                if (Build())
                {
                    RaymarchDebugger.Add(this);
                    SpatialStructureDebugger.Add(bvh);
                }
            }
            
            if (!executeInEditor && !Application.isPlaying)
            {
                SpatialStructureDebugger.Remove(bvh);
                RaymarchDebugger.Remove(this);
                Release();
            }

            if (IsInitialized)
            {
                RebuildIfNeeded();
            
                SetupRaymarchProperties(ref matInstance);
                if (debugMode != DebugModes.None)
                    SetupDebugProperties(ref matInstance);
            }
            
            void RebuildIfNeeded()
            {
                bool rebuild = matInstance.shader != (debugMode != DebugModes.None ? debugShader : mainShader);
                if (!rebuild) return;
                
                SpatialStructureDebugger.Remove(bvh);
                RaymarchDebugger.Remove(this);
                Release();
                
                if (Build())
                {
                    RaymarchDebugger.Add(this);
                    SpatialStructureDebugger.Add(bvh);
                }
            }
        }
        
        protected virtual void OnDrawGizmos()
        {
            if (!drawGizmos) return;
            
            if (!executeInEditor)
            {
                Gizmos.color = new Color(1, 1, 1, 0.3f);
                Gizmos.DrawSphere(transform.position, 0.1f);
            }

            bvh?.DrawStructure(showLabel);
        }

        protected void SetupDebugProperties(ref Material mat)
        {
            mat.SetInt(DebugModeId, (int)debugMode);
            mat.SetInt(BoundsDisplayThresholdId, boundsDisplayThreshold);
        }

        [ContextMenu("Find All Shapes")]
        protected void FindAllShapes()
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
        public static readonly int Stride = sizeof(float) * 33 + sizeof(int) * 2;
        
        public int Type;
        public Matrix4x4 Transform;
        public Vector3 Size;
        public Vector3 Pivot;
        public int Operation;
        public float Smoothness;
        public float Roundness;
        public Vector4 Color;
        public Vector4 EmissionColor;
        public float EmissionIntensity;

        public ShapeData(Transform sourceTransform, RaymarchShape.Setting setting)
        {
            Type = (int)setting.Shape;
            Transform = setting.UseLossyScale ? sourceTransform.worldToLocalMatrix : 
                Matrix4x4.TRS(sourceTransform.position, sourceTransform.rotation, Vector3.one).inverse;
            Size = setting.Size;
            Pivot = setting.Pivot;
            Operation = (int)setting.Operation;
            Smoothness = setting.Smoothness;
            Roundness = setting.Roundness;
            Color = setting.Color;
            EmissionColor = setting.EmissionColor;
            EmissionIntensity = setting.EmissionIntensity;
        }
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct NodeData<T> where T : struct, IBounds<T>
    {
        public static readonly int Stride = sizeof(float) * 6 + sizeof(int) * 2;
        
        public int Id;
        public T Bounds;
        public int ChildIndex;
    }
}