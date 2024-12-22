using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rayman
{
    [ExecuteInEditMode]
    public class ComputeRaymarchManager : MonoBehaviour
    {
        [SerializeField] private RaymarchFeature raymarchFeature;
        [SerializeField] private bool buildOnAwake;
        [SerializeField] private float boundsExpandSize;
        [SerializeField] private List<RaymarchGroup> raymarchGroups = new();
#if UNITY_EDITOR
        [SerializeField] private DebugModes debugMode = DebugModes.None;
        [SerializeField] private bool drawGizmos;
        [SerializeField] private bool showLabel;
        [SerializeField] private int boundsDisplayThreshold = 50;
#endif
        
        private ISpatialStructure<AABB> bvh;
        private List<BoundingVolume<AABB>> boundingVolumes;
        private ShapeData[] shapeData;
        private NodeData<AABB>[] nodeData;

        public ISpatialStructure<AABB> SpatialStructure => bvh;
        public int NodeCount => nodeData?.Length ?? 0;

        private void Awake()
        {
            if (raymarchFeature == null) return;
            
            if (!raymarchFeature.isActive)
                raymarchFeature.SetActive(true);
            
            if (buildOnAwake)
            {
                Build();
                SetupRaymarchProperties();
#if UNITY_EDITOR
                SetupDebugProperties();
#endif
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

        private void Update()
        {
            RaymarchRenderer.SyncBoundingVolumes<AABB>(ref bvh, ref boundingVolumes, boundsExpandSize);
            RaymarchRenderer.UpdateShapeData<AABB>(boundingVolumes, ref shapeData);
            RaymarchRenderer.UpdateNodeData<AABB>(bvh, ref nodeData);
        }

        [ContextMenu("Build")]
        public void Build()
        {
            if (raymarchGroups.Count == 0) return;
            
            bvh = new BVH<AABB>();
            boundingVolumes = new List<BoundingVolume<AABB>>();
            int shapeCount = 0;
            
            foreach (RaymarchGroup group in raymarchGroups)
            {
                if (group == null || !group.gameObject.activeInHierarchy) continue;
                
                foreach (RaymarchShape shape in group.Shapes)
                {
                    if (shape == null || !shape.gameObject.activeInHierarchy) continue;
                    
                    AABB bounds = shape.GetBounds<AABB>();
                    bvh.AddLeafNode(shapeCount, bounds, shape);
                    boundingVolumes.Add(new BoundingVolume<AABB>(shape, bounds));
                    shapeCount++;
                }
            }
            shapeData = new ShapeData[shapeCount];
            nodeData = new NodeData<AABB>[SpatialNode<AABB>.GetNodesCount(bvh.Root)];
        }

        public void AddRendererSafe(RaymarchGroup group)
        {
            if (raymarchGroups.Contains(group) || group.Shapes.Count == 0) return;
                
            raymarchGroups.Add(group);
            int id = boundingVolumes.Count;
            
            foreach (RaymarchShape shape in group.Shapes)
            {
                AABB bounds = shape.GetBounds<AABB>();
                bvh.AddLeafNode(id, bounds, shape);
                boundingVolumes.Add(new BoundingVolume<AABB>(shape, bounds));
                id++;
            }
            
            int count = boundingVolumes.Count;
            Array.Resize(ref shapeData, count);
            Array.Resize(ref nodeData, SpatialNode<AABB>.GetNodesCount(bvh.Root));
        }
        
        public void RemoveRenderer(RaymarchGroup group)
        {
            if (!raymarchGroups.Contains(group)) return;
                
            raymarchGroups.Remove(group);
            foreach (RaymarchShape shape in group.Shapes)
            {
                bvh.RemoveLeafNode(shape);
                int index = boundingVolumes.FindIndex(g => g.Source == shape);
                boundingVolumes.RemoveAt(index);
            }
            
            // update node Id to current index

            int count = boundingVolumes.Count;
            Array.Resize(ref shapeData, count);
            Array.Resize(ref nodeData, SpatialNode<AABB>.GetNodesCount(bvh.Root));
        }

        private ShapeData[] ProvideShapeData() => shapeData;

        private NodeData<AABB>[] ProvideNodeData() => nodeData;

        private void SetupRaymarchProperties()
        {
            if (raymarchFeature == null) return;
            raymarchFeature.Setting.SetTrigger();
        }
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (raymarchFeature == null)
            {
                raymarchFeature = Utilities.GetRendererFeature<RaymarchFeature>();
            }
            else
            {
                SetupRaymarchProperties();
                SetupDebugProperties();
            }
        }
        
        private void OnDrawGizmos()
        {
            if (bvh == null || !drawGizmos) return;

            bvh.DrawStructure(showLabel);
        }

        private void SetupDebugProperties()
        {
            if (raymarchFeature == null) return;

            raymarchFeature.Setting.DebugMode = debugMode;
            raymarchFeature.Setting.BoundsDisplayThreshold = boundsDisplayThreshold;
            raymarchFeature.Setting.SetTrigger();
        }

        [ContextMenu("Find All Groups")]
        private void FindAllGroups()
        {
            raymarchGroups = Utilities.GetChildrenByHierarchical<RaymarchGroup>();
        }
#endif
    }
}
