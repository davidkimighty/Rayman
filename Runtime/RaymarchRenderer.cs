using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
#if UNITY_EDITOR
using UnityEditor;
#endif
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
        public class MaterialGroup
        {
            public Material MaterialReference;
            public List<RaymarchShape> Shapes = new();
        }

        [Serializable]
        public class MaterialGroupData
        {
            public Material MaterialInstance;
            public BoundingVolume<AABB>[] BoundingVolumes;
            public ISpatialStructure<AABB> SpatialStructure;
            public ShapeData[] ShapeData;
            public NodeData<AABB>[] NodeData;
            public GraphicsBuffer ShapeBuffer;
            public GraphicsBuffer NodeBuffer;
        }
        
        public static readonly int ShapeBufferId = Shader.PropertyToID("_ShapeBuffer");
        public static readonly int NodeBufferId = Shader.PropertyToID("_NodeBuffer");
        public static readonly int MaxStepsId = Shader.PropertyToID("_MaxSteps");
        public static readonly int MaxDistanceId = Shader.PropertyToID("_MaxDistance");
        public static readonly int ShadowMaxStepsId = Shader.PropertyToID("_ShadowMaxSteps");
        public static readonly int ShadowMaxDistanceId = Shader.PropertyToID("_ShadowMaxDistance");
        public static readonly int DebugModeId = Shader.PropertyToID("_DebugMode");
        public static readonly int BoundsDisplayThresholdId = Shader.PropertyToID("_BoundsDisplayThreshold");

        [SerializeField] protected Renderer mainRenderer;
        [SerializeField] protected int maxSteps = 64;
        [SerializeField] protected float maxDistance = 100f;
        [SerializeField] protected int shadowMaxSteps = 32;
        [SerializeField] protected float shadowMaxDistance = 30f;
        [SerializeField] protected List<MaterialGroup> materialGroups = new();
#if UNITY_EDITOR
        [Header("Debugging")]
        [SerializeField] protected bool executeInEditor;
        [SerializeField] protected DebugModes debugMode = DebugModes.None;
        [SerializeField] protected bool drawGizmos;
        [SerializeField] protected int boundsDisplayThreshold = 300;
#endif
        protected List<MaterialGroupData> groupData = new();

        public List<MaterialGroupData> GroupData => groupData;
        public int ShapeCount => groupData?.Sum(g => g.BoundingVolumes.Length) ?? 0;
        public int SpatialStructureCount => groupData?.Count ?? 0;
        public int NodeCount => groupData?.Sum(g => g.SpatialStructure?.Count) ?? 0;
        public int MaxHeight => groupData?.Max(g => g.SpatialStructure?.MaxHeight) ?? 0;
        public bool IsInitialized => groupData != null && groupData.Count > 0;

        protected virtual void OnEnable()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying && !executeInEditor) return;
#endif
            if (Build())
            {
                RaymarchDebugger.Add(this);
                SpatialStructureDebugger.Add(this);
            }
        }
        
        protected virtual void LateUpdate()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying && !executeInEditor) return;
#endif
            if (!IsInitialized || !mainRenderer.isVisible) return;

            for (int i = 0; i < groupData.Count; i++)
            {
                MaterialGroupData data = groupData[i];
                RaymarchUtils.SyncBoundingVolumes(ref data.SpatialStructure, ref data.BoundingVolumes);
                RaymarchUtils.UpdateShapeData(data.BoundingVolumes, ref data.ShapeData);
                RaymarchUtils.FillNodeData(data.SpatialStructure, ref data.NodeData);
            
                data.ShapeBuffer?.SetData(data.ShapeData);
                data.NodeBuffer?.SetData(data.NodeData);
            }
        }
        
        protected virtual void OnDisable()
        {
            Release();
        }
        
        public virtual void AddShape(RaymarchShape shape, int groupId)
        {
            if (groupId >= materialGroups.Count || materialGroups[groupId].Shapes.Contains(shape)) return;
            
            materialGroups[groupId].Shapes.Add(shape);
        }

        public virtual void RemoveShape(RaymarchShape shape, int groupId)
        {
            if (groupId >= materialGroups.Count || !materialGroups[groupId].Shapes.Contains(shape)) return;
            
            int i = materialGroups[groupId].Shapes.FindIndex(b => b == shape);
            materialGroups[groupId].Shapes.RemoveAt(i);
        }
        
        [ContextMenu("Build")]
        public bool Build()
        {
            groupData = new List<MaterialGroupData>();
            foreach (MaterialGroup group in materialGroups)
            {
                List<RaymarchShape> activeShapes = group.Shapes
                    .Where(s => s != null && s.gameObject.activeInHierarchy).ToList();
                int shapeCount = activeShapes.Count;
                if (shapeCount == 0) continue;
                
                MaterialGroupData data = new();
                data.MaterialInstance = CreateMaterial(group.MaterialReference);
                data.BoundingVolumes = RaymarchUtils.CreateBoundingVolumes<AABB>(activeShapes).ToArray();
                data.SpatialStructure = RaymarchUtils.CreateSpatialStructure(data.BoundingVolumes);
            
                data.ShapeData = new ShapeData[shapeCount];
                SetupShapeBuffer(shapeCount, ref data.MaterialInstance, ref data.ShapeBuffer);
            
                int nodesCount = SpatialNode<AABB>.GetNodesCount(data.SpatialStructure.Root);
                data.NodeData = new NodeData<AABB>[nodesCount];
                SetupNodeBuffer(nodesCount, ref data.MaterialInstance, ref data.NodeBuffer);
            
                SetupRaymarchProperties(ref data.MaterialInstance);
                if (debugMode != DebugModes.None)
                    SetupDebugProperties(ref data.MaterialInstance);
                groupData.Add(data);
            }
            mainRenderer.materials = groupData.Select(g => g.MaterialInstance).ToArray();
            return true;
        }

        public void Release()
        {
            if (!IsInitialized) return;
            
            for (int i = 0; i < groupData.Count; i++)
            {
                MaterialGroupData data = groupData[i];
                data.ShapeBuffer?.Release();
                data.NodeBuffer?.Release();

                if (data.MaterialInstance != null)
                {
                    if (Application.isPlaying)
                        Destroy(data.MaterialInstance);
                    else
                        DestroyImmediate(data.MaterialInstance);
                    data.MaterialInstance = null;
                }
            }
            groupData.Clear();
            mainRenderer.materials = Array.Empty<Material>();
        }

        protected Material CreateMaterial(Material matRef)
        {
#if UNITY_EDITOR
            if (debugMode != DebugModes.None)
                return CoreUtils.CreateEngineMaterial("Rayman/RaymarchDebugLit");
#endif
            return matRef != null ? new Material(matRef) : CoreUtils.CreateEngineMaterial("Rayman/RaymarchLit");
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
            if (mat == null) return;
            
            mat.SetInt(MaxStepsId, maxSteps);
            mat.SetFloat(MaxDistanceId, maxDistance);
            mat.SetInt(ShadowMaxStepsId, shadowMaxSteps);
            mat.SetFloat(ShadowMaxDistanceId, shadowMaxDistance);
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
                    SpatialStructureDebugger.Add(this);
                }
            }
            
            if (!executeInEditor && !Application.isPlaying)
            {
                SpatialStructureDebugger.Remove(this);
                RaymarchDebugger.Remove(this);
                Release();
            }

            if (IsInitialized)
            {
                for (int i = 0; i < groupData.Count; i++)
                {
                    SetupRaymarchProperties(ref groupData[i].MaterialInstance);
                    if (debugMode != DebugModes.None)
                        SetupDebugProperties(ref groupData[i].MaterialInstance);
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

            if (IsInitialized)
            {
                for (int i = 0; i < groupData.Count; i++)
                    groupData[i].SpatialStructure?.DrawStructure();
            }
        }

        protected void SetupDebugProperties(ref Material mat)
        {
            if (mat == null) return;
            
            mat.SetInt(DebugModeId, (int)debugMode);
            mat.SetInt(BoundsDisplayThresholdId, boundsDisplayThreshold);
        }

        [ContextMenu("Find All Shapes")]
        protected void FindAllShapes()
        {
            materialGroups.Add(new MaterialGroup
            {
                MaterialReference = null,
                Shapes = RaymarchUtils.GetChildrenByHierarchical<RaymarchShape>(transform) 
            });
            EditorUtility.SetDirty(this);
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