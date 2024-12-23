using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Rayman
{
#if UNITY_EDITOR
    public enum DebugModes { None, Color, Normal, Hitmap, BoundingVolume, }
#endif
    [ExecuteInEditMode]
    public abstract class RaymarchRenderer : MonoBehaviour
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
        [SerializeField] protected float shadowBias = 0.013f;
#if UNITY_EDITOR
        [Header("Debugging")] [SerializeField] protected Shader debugShader;
        [SerializeField] protected DebugModes debugMode = DebugModes.None;
        [SerializeField] protected bool drawGizmos;
        [SerializeField] protected bool showLabel;
        [SerializeField] protected int boundsDisplayThreshold = 300;
#endif
        
        public static BVH<T> CreateSpatialStructure<T>(BoundingVolume<T>[] volumes) where T : struct, IBounds<T>
        {
            BVH<T> spatialStructure = new();
            int shapeId = 0;
            
            for (int i = 0; i < volumes.Length; i++)
            {
                BoundingVolume<T> volume = volumes[i];
                if (volume == null) continue;
                
                spatialStructure.AddLeafNode(shapeId, volume.Bounds, volume.Source);
                shapeId++;
            }
            return spatialStructure;
        }

        public static void SyncBoundingVolumes<T>(ref ISpatialStructure<T> spatialStructure,
            ref BoundingVolume<T>[] boundingVolumes) where T : struct, IBounds<T>
        {
            for (int i = 0; i < boundingVolumes.Length; i++)
            {
                BoundingVolume<T> volume = boundingVolumes[i];
                T buffBounds = volume.Bounds.Expand(volume.Source.Settings.BoundsExpandSize);
                T newBounds = volume.Source.GetBounds<T>();
                if (buffBounds.Contains(newBounds)) continue;

                volume.Bounds = newBounds;
                spatialStructure.UpdateBounds(volume.Source, newBounds);
            }
        }

        public static void UpdateShapeData<T>(BoundingVolume<T>[] boundingVolumes,
            ref ShapeData[] shapeData) where T : struct, IBounds<T>
        {
            for (int i = 0; i < shapeData.Length; i++)
            {
                RaymarchShape shape = boundingVolumes[i].Source;
                if (shape == null) continue;
                
                shapeData[i] = new ShapeData(shape.transform, shape.Settings);
            }
        }

        public static void UpdateOperationData<T>(BoundingVolume<T>[] boundingVolumes,
            ref DistortionData[] distortionData) where T : struct, IBounds<T>
        {
            for (int i = 0; i < boundingVolumes.Length; i++)
            {
                RaymarchShape.Distortion distortion = boundingVolumes[i].Source.Settings.Distortion;
                if (!distortion.Enabled) continue;

                distortionData[i] = new DistortionData(i, (int)distortion.Type, distortion.Amount);
            }
        }

        public static void FillNodeData<T>(ISpatialStructure<T> spatialStructure,
            ref NodeData<T>[] nodeData) where T : struct, IBounds<T>
        {
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
        
        public List<BoundingVolume<T>> CreateBoundingVolumes<T>() where T : struct, IBounds<T>
        {
            var volumes = new List<BoundingVolume<T>>();
            foreach (RaymarchShape shape in shapes)
            {
                if (shape == null || !shape.gameObject.activeInHierarchy) continue;
                
                T bounds = shape.GetBounds<T>();
                volumes.Add(new BoundingVolume<T>(shape, bounds));
            }
            return volumes;
        }
        
#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            if (mainRenderer == null)
                mainRenderer = GetComponent<Renderer>();
        }

        [ContextMenu("Find All Shapes")]
        private void FindAllShapes()
        {
            shapes = Utilities.GetChildrenByHierarchical<RaymarchShape>(transform);
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