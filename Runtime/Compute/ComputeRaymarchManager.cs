using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Rayman
{
    [ExecuteInEditMode]
    public class ComputeRaymarchManager : MonoBehaviour, IComputeRaymarchDataProvider
    {
        [SerializeField] private ComputeRaymarchFeature raymarchFeature;
        [SerializeField] private List<ComputeRaymarchRenderer> raymarchRenderers = new();
        [SerializeField] private bool buildOnAwake;
        [SerializeField] private float boundsUpdateThreshold;
#if UNITY_EDITOR
        [SerializeField] private DebugModes debugMode = DebugModes.None;
        [SerializeField] private bool drawGizmos;
        [SerializeField] private int boundsDisplayThreshold = 1300;
#endif
        private ISpatialStructure<AABB> bvh;
        private BoundingVolume<AABB>[] boundingVolumes;
        private ShapeData[] shapeData;
        private NodeDataAABB[] nodeData;

        private void Awake()
        {
            if (raymarchFeature == null) return;
            
            if (!raymarchFeature.isActive)
                raymarchFeature.SetActive(true);
            
            if (buildOnAwake)
            {
                Build();
#if UNITY_EDITOR
                SetupDebugProperties();
#endif
            }
            raymarchFeature.PassDataProvider = this;
        }

        private void LateUpdate()
        {
            if (boundingVolumes == null || bvh == null) return;
            
            for (int j = 0; j < boundingVolumes.Length; j++)
            {
                BoundingVolume<AABB> volume = boundingVolumes[j];
                var shape = volume.Source as RaymarchShape;
                 if (shape != null)
                     shapeData[j] = new ShapeData(shape);
                volume.SyncVolume(ref bvh, boundsUpdateThreshold);
            }
            UpdateNodeData(bvh, ref nodeData);
        }

        public ShapeData[] GetShapeData() => shapeData;

        public NodeDataAABB[] GetNodeData() => nodeData;

        [ContextMenu("Build")]
        public bool Build()
        {
            if (raymarchRenderers.Count == 0) return false;
            
            List<BoundingVolume<AABB>> volumes = new();
            foreach (ComputeRaymarchRenderer rr in raymarchRenderers)
            {
                if (rr == null || !rr.gameObject.activeInHierarchy) continue;
                
                var activeShapes = rr.Shapes.Where(s => s != null && s.gameObject.activeInHierarchy).ToList();
                if (activeShapes.Count == 0) continue;
                
                volumes.AddRange(rr.Shapes.Select(e => new BoundingVolume<AABB>(e)).ToArray());
            }
            if (volumes.Count == 0) return false;
            
            boundingVolumes = volumes.ToArray();
            bvh = Bvh<AABB>.Create(boundingVolumes);
            
            int shapeCount = boundingVolumes.Length;
            shapeData = new ShapeData[shapeCount];
            
            int nodesCount = SpatialNode<AABB>.GetNodesCount(bvh.Root);
            nodeData = new NodeDataAABB[nodesCount];
            return true;
        }
        
        private void UpdateNodeData(ISpatialStructure<AABB> structure, ref NodeDataAABB[] nodeData)
        {
            int index = 0;
            Queue<(SpatialNode<AABB> node, int parentIndex)> queue = new();
            queue.Enqueue((structure.Root, -1));

            while (queue.Count > 0)
            {
                (SpatialNode<AABB> current, int parentIndex) = queue.Dequeue();
                NodeDataAABB data = new()
                {
                    Id = current.Id,
                    ChildIndex = -1,
                    Bounds = current.Bounds,
                };

                if (current.LeftChild != null)
                {
                    data.ChildIndex = index + queue.Count + 1;
                    queue.Enqueue((current.LeftChild, index));
                }
                if (current.RightChild != null)
                    queue.Enqueue((current.RightChild, index));
                
                nodeData[index] = data;
                index++;
            }
        }
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (raymarchFeature == null)
                raymarchFeature = RaymarchUtils.GetRendererFeature<ComputeRaymarchFeature>();
            else
            {
                SetupDebugProperties();
            }
        }
        
        private void OnDrawGizmos()
        {
            if (bvh == null || !drawGizmos) return;

            bvh.DrawStructure();
        }

        private void SetupDebugProperties()
        {
            if (raymarchFeature == null) return;

            raymarchFeature.Setting.DebugMode = debugMode;
            raymarchFeature.Setting.BoundsDisplayThreshold = boundsDisplayThreshold;
            raymarchFeature.Setting.SetTrigger();
        }

        [ContextMenu("Find All Raymarch Renderers")]
        private void FindAllGroups()
        {
            raymarchRenderers = RaymarchUtils.GetChildrenByHierarchical<ComputeRaymarchRenderer>();
        }
#endif
    }
}
