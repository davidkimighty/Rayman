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
    public class RaymarchRenderer : MonoBehaviour
    {
        public static readonly int ShapeBufferId = Shader.PropertyToID("_ShapeBuffer");
        public static readonly int DistortionCountId = Shader.PropertyToID("_DistortionCount");
        public static readonly int DistortionBufferId = Shader.PropertyToID("_DistortionBuffer");
        public static readonly int NodeBufferId = Shader.PropertyToID("_NodeBuffer");
        public static readonly int MaxStepsId = Shader.PropertyToID("_MaxSteps");
        public static readonly int MaxDistanceId = Shader.PropertyToID("_MaxDistance");
        public static readonly int ShadowBiasId = Shader.PropertyToID("_ShadowBiasVal");
        private static readonly int DebugModeId = Shader.PropertyToID("_DebugMode");
        private static readonly int BoundsDisplayThresholdId = Shader.PropertyToID("_BoundsDisplayThreshold");

        [Header("Raymarch Surface Shader")]
        [SerializeField] private Renderer mainRenderer;
        [SerializeField] private Shader mainShader;
        [SerializeField] private int maxSteps = 64;
        [SerializeField] private float maxDistance = 100f;
        [SerializeField] private float shadowBias = 0.1f;
        [SerializeField] private float boundsExpandSize;
        [SerializeField] private List<RaymarchShape> shapes = new();
#if UNITY_EDITOR
        [Header("Debugging")]
        [SerializeField] private Shader debugShader;
        [SerializeField] private DebugModes debugMode = DebugModes.None;
        [SerializeField] private bool drawGizmos;
        [SerializeField] private bool showLabel;
        [SerializeField] private int boundsDisplayThreshold = 1300;
#endif
        private Material mat;
        private GraphicsBuffer shapeBuffer;
        private GraphicsBuffer distortionBuffer;
        private GraphicsBuffer nodeBuffer;
        private ShapeData[] shapeData;
        private DistortionData[] distortionData;
        private NodeData<AABB>[] nodeData;
        private ISpatialStructure<AABB> bvh;
        private List<BoundingVolume<AABB>> boundingVolumes;

        public List<RaymarchShape> Shapes => shapes;
        
        private void Awake()
        {
            Build();
        }

        private void Update()
        {
            SyncBoundingVolumes<AABB>(ref bvh, ref boundingVolumes, boundsExpandSize);
            UpdateShapeData<AABB>(boundingVolumes, ref shapeData);
            UpdateOperationData<AABB>(boundingVolumes, ref distortionData);
            UpdateNodeData<AABB>(bvh, ref nodeData);
            
            shapeBuffer?.SetData(shapeData);
            distortionBuffer?.SetData(distortionData);
            nodeBuffer?.SetData(nodeData);
        }

        public static void SyncBoundingVolumes<T>(ref ISpatialStructure<T> spatialStructure,
            ref List<BoundingVolume<T>> boundingVolumes, float boundsExpandSize = 0f) where T : struct, IBounds<T>
        {
            if (boundingVolumes == null) return;

            foreach (BoundingVolume<T> volume in boundingVolumes)
            {
                T buffBounds = volume.Bounds.Expand(boundsExpandSize);
                T newBounds = volume.Source.GetBounds<T>();
                if (buffBounds.Contains(newBounds)) continue;
                
                volume.Bounds = newBounds;
                spatialStructure.UpdateBounds(volume.Source, newBounds);
            }
        }
        
        public static void UpdateShapeData<T>(List<BoundingVolume<T>> boundingVolumes,
            ref ShapeData[] shapeData) where T : struct, IBounds<T>
        {
            if (shapeData == null) return;
            
            for (int i = 0; i < shapeData.Length; i++)
            {
                RaymarchShape shape = boundingVolumes[i].Source;
                if (shape == null) continue;

                Transform sourceTransform = shape.transform;
                RaymarchShape.Setting setting = shape.Settings;
                shapeData[i] = new ShapeData(sourceTransform, setting);
            }
        }
        
        public static void UpdateOperationData<T>(List<BoundingVolume<T>> boundingVolumes,
            ref DistortionData[] distortionData) where T : struct, IBounds<T>
        {
            if (distortionData == null) return;

            int j = 0;
            for (int i = 0; i < boundingVolumes.Count; i++)
            {
                RaymarchShape.Distortion distortion = boundingVolumes[i].Source.Settings.Distortion;
                if (!distortion.Enabled) continue;

                distortionData[j] = new DistortionData(i, (int)distortion.Type, distortion.Amount);
                j++;
            }
        }
        
        public static void UpdateNodeData<T>(ISpatialStructure<T> spatialStructure,
            ref NodeData<T>[] nodeData) where T : struct, IBounds<T>
        {
            if (spatialStructure == null) return;

            int index = 0;
            Queue<(SpatialNode<T> node, int parentIndex)> queue = new();
            queue.Enqueue((spatialStructure.Root, -1));

            while (queue.Count > 0)
            {
                (SpatialNode<T> current, int parentIndex) = queue.Dequeue();
                NodeData<T> data = new()
                {
                    Id = current.Id,
                    Bounds = current.Bounds,
                    Parent = parentIndex,
                    Left = -1,
                    Right = -1,
                };

                if (current.LeftChild != null)
                {
                    queue.Enqueue((current.LeftChild, index));
                    data.Left = index + queue.Count;
                }
                if (current.RightChild != null)
                {
                    queue.Enqueue((current.RightChild, index));
                    data.Right = index + queue.Count;
                }
                nodeData[index] = data;
                index++;
            }
        }

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
            SetupRaymarchProperties(mat);
            mainRenderer.material = mat;
            
            SetupShapeBuffer(mat);
            SetupDistortionBuffer(mat);
            
            SetupSpatialStructure();
            SetupNodeBuffer(mat);

#if UNITY_EDITOR
            void SetupMaterialInEditor()
            {
                if (mat != null) return;
                if (debugMode != DebugModes.None)
                {
                    if (debugShader == null) return;
                    mat = CoreUtils.CreateEngineMaterial(debugShader);
                    SetupDebugProperties(mat);
                }
            }
#endif
        }

        private void SetupRaymarchProperties(Material mat)
        {
            mat.SetInt(MaxStepsId, maxSteps);
            mat.SetFloat(MaxDistanceId, maxDistance);
            mat.SetFloat(ShadowBiasId, shadowBias);
        }

        private void SetupShapeBuffer(Material mat)
        {
            shapeBuffer?.Release();
            int count = shapes.Count;
            if (count == 0) return;
            
            shapeBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, count, Marshal.SizeOf(typeof(ShapeData)));
            shapeData = new ShapeData[count];
        
            mat.SetBuffer(ShapeBufferId, shapeBuffer);
        }

        private void SetupDistortionBuffer(Material mat)
        {
            distortionBuffer?.Release();
            int count = shapes.Count(s => s.Settings.Distortion.Enabled);
            if (count == 0) return;
            
            distortionBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, count, Marshal.SizeOf(typeof(DistortionData)));
            distortionData = new DistortionData[count];
            
            mat.SetInt(DistortionCountId, count);
            mat.SetBuffer(DistortionBufferId, distortionBuffer);
            mat.EnableKeyword("_DISTORTION_FEATURE");
        }

        private void SetupNodeBuffer(Material mat)
        {
            if (bvh == null) return;
            
            nodeBuffer?.Release();
            int count = SpatialNode<AABB>.GetNodesCount(bvh.Root);
            if (count == 0) return;

            nodeBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, count, Marshal.SizeOf(typeof(NodeData<AABB>)));
            nodeData = new NodeData<AABB>[count];
            mat.SetBuffer(NodeBufferId, nodeBuffer);
        }

        private void SetupSpatialStructure()
        {
            bvh = new BVH<AABB>();
            boundingVolumes = new List<BoundingVolume<AABB>>();
            int shapeCount = 0;
            
            foreach (RaymarchShape shape in shapes)
            {
                if (shape == null || !shape.gameObject.activeInHierarchy) continue;
                    
                AABB bounds = shape.GetBounds<AABB>();
                bvh.AddLeafNode(shapeCount, bounds, shape);
                boundingVolumes.Add(new BoundingVolume<AABB>(shape, bounds));
                shapeCount++;
            }
        }
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (mainRenderer == null)
                mainRenderer = GetComponent<Renderer>();

            if (mat == null) return;
            
            bool rebuild = mat.shader != (debugMode != DebugModes.None ? debugShader : mainShader);
            if (rebuild)
            {
                DestroyImmediate(mat);
                mat = null;
                Build();
                return;
            }
            
            SetupRaymarchProperties(mat);
            if (debugMode != DebugModes.None)
                SetupDebugProperties(mat);
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
                SyncBoundingVolumes<AABB>(ref bvh, ref boundingVolumes, boundsExpandSize);
                UpdateShapeData<AABB>(boundingVolumes, ref shapeData);
                UpdateOperationData<AABB>(boundingVolumes, ref distortionData);
                UpdateNodeData<AABB>(bvh, ref nodeData);
            }
        }
        
        private void SetupDebugProperties(Material mat)
        {
            mat.SetInt(DebugModeId, (int)debugMode);
            mat.SetInt(BoundsDisplayThresholdId, boundsDisplayThreshold);
        }
        
        [ContextMenu("Reset Shape Buffer")]
        public void ResetShapeBuffer()
        {
            Build();
            SyncBoundingVolumes<AABB>(ref bvh, ref boundingVolumes, boundsExpandSize);
            UpdateShapeData<AABB>(boundingVolumes, ref shapeData);
            UpdateOperationData<AABB>(boundingVolumes, ref distortionData);
            UpdateNodeData<AABB>(bvh, ref nodeData);
        }

        [ContextMenu("Find All Shapes")]
        private void FindAllShapes()
        {
            shapes = Utilities.GetChildrenByHierarchical<RaymarchShape>(transform);
        }
#endif
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct ShapeData
    {
        public Matrix4x4 Transform;
        public Vector3 LossyScale;
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
            Transform = sourceTransform.worldToLocalMatrix;
            LossyScale = sourceTransform.lossyScale;
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