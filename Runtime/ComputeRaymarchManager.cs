using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
#endif
using UnityEngine;
using Random = UnityEngine.Random;

namespace Rayman
{
    //[ExecuteInEditMode]
    public class ComputeRaymarchManager : MonoBehaviour
    {
#if UNITY_EDITOR
        public enum DebugModes { None, Color, Normal, Hitmap, BoundingVolume, }
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
        
        [SerializeField] private RaymarchFeature raymarchFeature;
        [SerializeField] private bool buildOnAwake;
        [SerializeField] private float boundsBuffSize;
        [SerializeField] private int boundsSyncGroupSize = 100;
        [SerializeField] private List<ComputeRaymarchRenderer> raymarchRenderers = new();
#if UNITY_EDITOR
        [SerializeField] private DebugModes debugMode;
        [SerializeField] private bool drawGizmos;
        [SerializeField] private bool showLabel;
#endif
        
        private BVH<AABB> bvh;
        private List<BoundingVolume> boundingVolumes;
        private ComputeShapeData[] shapeData;
        private NodeData[] nodeData; 
        private IEnumerator syncBounds;
#if UNITY_EDITOR
        private Color[] depthColors;
#endif

        public BVH<AABB> SpatialStructure => bvh;

        private void Awake()
        {
            if (raymarchFeature == null) return;
            
            if (!raymarchFeature.isActive)
                raymarchFeature.SetActive(true);
            
#if RAYMARCH_DEBUG
            raymarchFeature.SetDebugMode((int)debugMode);
#endif
            if (buildOnAwake)
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
                if (raymarchRenderer == null || !raymarchRenderer.gameObject.activeInHierarchy) continue;
                
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
            nodeData = new NodeData[SpatialNode<AABB>.GetNodesCount(bvh.Root)];
            
            if (syncBounds != null)
                StopCoroutine(syncBounds);
            syncBounds = SyncBoundingVolumes();
            StartCoroutine(syncBounds);
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
        }

        private ComputeShapeData[] ProvideShapeData()
        {
            if (boundingVolumes == null) return null;
            
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
            return shapeData;
        }

        private NodeData[] ProvideNodeData()
        {
            if (bvh == null) return null;
            
            Queue<(SpatialNode<AABB> node, int parentIndex)> queue = new();
            queue.Enqueue((bvh.Root, -1));
            int currentIndex = 0;

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

                nodeData[currentIndex] = data;
                currentIndex++;

                if (current.LeftChild != null)
                {
                    queue.Enqueue((current.LeftChild, currentIndex - 1));
                    nodeData[currentIndex].Left = currentIndex;
                }
                if (current.RightChild != null)
                {
                    queue.Enqueue((current.RightChild, currentIndex - 1));
                    nodeData[currentIndex].Right = currentIndex;
                }
            }
            return nodeData;
        }

        private IEnumerator SyncBoundingVolumes()
        {
            if (boundingVolumes == null) yield break;

            while (true)
            {
                int syncCount = 0;
                foreach (BoundingVolume gd in boundingVolumes)
                {
                    if (gd == null) continue;
                
                    AABB buffBounds = gd.Bounds.Expand(boundsBuffSize);
                    AABB newBounds = gd.Source.GetBounds<AABB>();
                    if (buffBounds.Contains(newBounds)) continue;
                
                    gd.Bounds = newBounds;
                    bvh.UpdateBounds(gd.Source, newBounds);
                
                    syncCount++;
                    if (syncCount == boundsSyncGroupSize)
                    {
                        syncCount = 0;
                        yield return null;
                    }
                }
                yield return null;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (debugMode != DebugModes.None)
                AddDefineSymbol(RaymarchFeature.DebugKeyword);
            else
                RemoveDefineSymbol(RaymarchFeature.DebugKeyword);
        }
        
        private void OnDrawGizmos()
        {
            if (bvh == null || !drawGizmos) return;

            if (depthColors == null)
            {
                depthColors = new Color[30];
                for (int i = 0; i < depthColors.Length; i++)
                    depthColors[i] = Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f);
            }
            bvh.DrawStructure(showLabel, depthColors);
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
            raymarchRenderers = Utilities.GetChildrenByHierarchical<ComputeRaymarchRenderer>();
        }
#endif
    }
}
