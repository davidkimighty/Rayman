using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
#endif
using UnityEngine;
using Random = UnityEngine.Random;

namespace Rayman
{
    [ExecuteInEditMode]
    public class ComputeRaymarchManager : MonoBehaviour
    {
#if UNITY_EDITOR
        public enum DebugMode { None, Color, Normal, Hitmap, BoundingVolume, }
#endif
        public class BoundingVolume
        {
            public RaymarchShape Source;
            public AABB Bounds;

            public BoundingVolume(RaymarchShape shape, AABB aabb)
            {
                Source = shape;
                Bounds = aabb;
            }
        }
        
        [Serializable]
        public struct Setting
        {
            public bool BuildOnAwake;
            public float BoundsBuffSize;
#if UNITY_EDITOR
            public DebugMode DebugMode;
            public bool DrawGizmos;
#endif
        }
        
        [SerializeField] private Setting setting;
        [SerializeField] private RaymarchFeature raymarchFeature;
        [SerializeField] private List<ComputeRaymarchRenderer> raymarchRenderers = new();
        
        private BVH<AABB> bvh;
        private List<BoundingVolume> boundingVolumes;
        private ComputeShapeData[] shapeData;
        private NodeData[] nodeData;
#if UNITY_EDITOR
        private Color[] depthColors;
#endif

        private void Awake()
        {
            if (raymarchFeature == null) return;
            
            if (!raymarchFeature.isActive)
                raymarchFeature.SetActive(true);
            
#if RAYMARCH_DEBUG
            raymarchFeature.SetDebugMode((int)setting.DebugMode);
#endif
            if (setting.BuildOnAwake)
            {
                if (!Build())
                    Debug.LogWarning("Failed to build raymarch data.");
            }
        }

        private void OnEnable()
        {
            raymarchFeature.OnRequestShapeData += ProvideShapeData;
            raymarchFeature.OnRequestNodeData += ProvideNodeData;
        }

        private void Update()
        {
            SyncBoundingVolumes();
            UpdateShapeData();
            UpdateNodeData();
        }
        
        private void OnDisable()
        {
            raymarchFeature.OnRequestShapeData -= ProvideShapeData;
            raymarchFeature.OnRequestNodeData -= ProvideNodeData;
        }

        public bool Build()
        {
            if (raymarchRenderers.Count == 0) return false;
            
            bvh = new BVH<AABB>();
            boundingVolumes = new List<BoundingVolume>();
            int shapeCount = 0;
            
            foreach (ComputeRaymarchRenderer raymarchRenderer in raymarchRenderers)
            {
                if (raymarchRenderer == null) continue;
                
                foreach (RaymarchShape shape in raymarchRenderer.Shapes)
                {
                    if (shape == null) continue;
                    
                    AABB bounds = shape.GetBounds<AABB>();
                    bvh.AddLeafNode(shapeCount, bounds, shape);
                    boundingVolumes.Add(new BoundingVolume(shape, bounds));
                    shapeCount++;
                }
            }
            shapeData = new ComputeShapeData[shapeCount];
            nodeData = new NodeData[bvh.Count + 1];
            return true;
        }

        public void AddRendererSafe(ComputeRaymarchRenderer renderer)
        {
            if (raymarchRenderers.Contains(renderer) || renderer.Shapes.Count == 0) return;
                
            raymarchRenderers.Add(renderer);
            int id = boundingVolumes.Count;
            
            foreach (RaymarchShape shape in renderer.Shapes)
            {
                AABB bounds = shape.GetBounds<AABB>();
                bvh.AddLeafNode(id, bounds, shape);
                boundingVolumes.Add(new BoundingVolume(shape, bounds));
                id++;
            }
            
            int count = boundingVolumes.Count;
            Array.Resize(ref shapeData, count);
            Array.Resize(ref nodeData, count);
        }
        
        public void RemoveRenderer(ComputeRaymarchRenderer renderer)
        {
            if (!raymarchRenderers.Contains(renderer)) return;
                
            raymarchRenderers.Remove(renderer);
            foreach (RaymarchShape shape in renderer.Shapes)
            {
                bvh.RemoveLeafNode(shape);
                int index = boundingVolumes.FindIndex(g => g.Source == shape);
                boundingVolumes.RemoveAt(index);
            }
            
            // update node Id to current index

            int count = boundingVolumes.Count;
            Array.Resize(ref shapeData, count);
            Array.Resize(ref nodeData, count);
        }
        
        private ComputeShapeData[] ProvideShapeData() => shapeData;

        private NodeData[] ProvideNodeData() => nodeData;

        private void SyncBoundingVolumes()
        {
            if (boundingVolumes == null) return;

            foreach (BoundingVolume gd in boundingVolumes)
            {
                if (gd == null) continue;
                
                AABB buffBounds = gd.Bounds.Expand(setting.BoundsBuffSize);
                AABB newBounds = gd.Source.GetBounds<AABB>();
                if (buffBounds.Contains(newBounds)) continue;
                
                gd.Bounds = newBounds;
                bvh.UpdateBounds(gd.Source, newBounds);
            }
        }
        
        private void UpdateShapeData()
        {
            if (boundingVolumes == null) return;

            int groupId = 0;
            RaymarchShape currentSource = boundingVolumes[0].Source;
            
            for (int i = 0; i < boundingVolumes.Count; i++)
            {
                BoundingVolume gd = boundingVolumes[i];
                if (gd == null) continue;

                if (currentSource != gd.Source)
                {
                    currentSource = gd.Source;
                    groupId++;
                }
                Transform sourceTransform = gd.Source.transform;
                RaymarchShape.Setting settings = gd.Source.Settings;
                shapeData[i] = new ComputeShapeData(groupId, i, sourceTransform, settings);
            }
        }

        private void UpdateNodeData()
        {
            if (bvh == null) return;
            
            Queue<(SpatialNode<AABB> node, int parentIndex)> queue = new();
            queue.Enqueue((bvh.Root, -1));
            int count = 0;
            
            while (queue.Count > 0)
            {
                (SpatialNode<AABB> current, int parentIndex) = queue.Dequeue();
                NodeData data = new()
                {
                    Id = current.Id,
                    Bounds = current.Bounds,
                    Parent = parentIndex,
                    Left = -1, Right = -1,
                };
            
                int currentIndex = count;
                if (current.LeftChild != null)
                {
                    queue.Enqueue((current.LeftChild, currentIndex));
                    data.Left = count + queue.Count;
                }
                if (current.RightChild != null)
                {
                    queue.Enqueue((current.RightChild, currentIndex));
                    data.Right = count + queue.Count;
                }
                
                nodeData[count] = data;
                count++;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (setting.DebugMode != DebugMode.None)
                AddDefineSymbol(RaymarchFeature.DebugKeyword);
            else
                RemoveDefineSymbol(RaymarchFeature.DebugKeyword);
        }
        
        private void OnDrawGizmos()
        {
            if (bvh == null || !setting.DrawGizmos) return;

            if (depthColors == null)
            {
                depthColors = new Color[10];
                for (int i = 0; i < depthColors.Length; i++)
                    depthColors[i] = Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f);
            }
            bvh.DrawStructure(depthColors);
        }

        public static void AddDefineSymbol(string symbol)
        {
            string currentSymbols = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone);
            if (!currentSymbols.Contains(symbol))
                PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Standalone, symbol);
        }

        public static void RemoveDefineSymbol(string symbol)
        {
            string currentSymbols = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone);
            if (currentSymbols.Contains(symbol))
            {
                PlayerSettings.SetScriptingDefineSymbols(
                    NamedBuildTarget.Standalone, 
                    currentSymbols.Replace(symbol + ";", "").Replace(symbol, "")
                );
            }
        }
        
        [ContextMenu("Find All Renderers")]
        private void FindAllRenderers()
        {
            raymarchRenderers = Utilities.GetObjectsByTypes<ComputeRaymarchRenderer>();
        }
        
        public static NodeData[] GetGeometriesData(SpatialNode<AABB> root)
        {
            if (root == null) return null;
            
            List<NodeData> result = new();
            Queue<(SpatialNode<AABB> node, int parentIndex)> queue = new();
            queue.Enqueue((root, -1));
            
            while (queue.Count > 0)
            {
                (SpatialNode<AABB> current, int parentIndex) = queue.Dequeue();
                NodeData data = new()
                {
                    Id = current.Id,
                    Bounds = current.Bounds,
                    Parent = parentIndex,
                    Left = -1, Right = -1,
                };
                
                int currentIndex = result.Count;
                if (current.LeftChild != null)
                {
                    queue.Enqueue((current.LeftChild, currentIndex));
                    data.Left = result.Count + queue.Count;
                }
                if (current.RightChild != null)
                {
                    queue.Enqueue((current.RightChild, currentIndex));
                    data.Right = result.Count + queue.Count;
                }
                result.Add(data);
            }
            return result.ToArray();
        }
#endif
    }
}
