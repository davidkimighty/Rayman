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
#if UNITY_EDITOR
        [SerializeField] private DebugModes debugMode = DebugModes.None;
        [SerializeField] private bool drawGizmos;
        [SerializeField] private int boundsDisplayThreshold = 1300;
#endif
        private ISpatialStructure<AABB> bvh;
        private BoundingVolume<AABB>[] boundingVolumes;
        private ShapeData[] shapeData;
        private NodeData<AABB>[] nodeData;

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
            
            RaymarchUtils.SyncBoundingVolumes(ref bvh, ref boundingVolumes);
            RaymarchUtils.UpdateShapeData(boundingVolumes, ref shapeData);
            RaymarchUtils.FillNodeData(bvh, ref nodeData);
        }

        public ShapeData[] GetShapeData() => shapeData;

        public NodeData<AABB>[] GetNodeData() => nodeData;

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
                
                volumes.AddRange(RaymarchUtils.CreateBoundingVolumes<AABB>(rr.Shapes));
            }
            if (volumes.Count == 0) return false;
            
            boundingVolumes = volumes.ToArray();
            bvh = RaymarchUtils.CreateSpatialStructure<AABB>(boundingVolumes);
            
            int shapeCount = boundingVolumes.Length;
            shapeData = new ShapeData[shapeCount];
            
            int nodesCount = SpatialNode<AABB>.GetNodesCount(bvh.Root);
            nodeData = new NodeData<AABB>[nodesCount];
            return true;
        }
        
        public void AddRaymarchRenderer(ComputeRaymarchRenderer raymarchRenderer)
        {
            if (raymarchRenderers.Contains(raymarchRenderer)) return;
            
            raymarchRenderers.Add(raymarchRenderer);
        }

        public void RemoveRaymarchRenderer(ComputeRaymarchRenderer raymarchRenderer)
        {
            if (!raymarchRenderers.Contains(raymarchRenderer)) return;

            int i = raymarchRenderers.IndexOf(raymarchRenderer);
            raymarchRenderers.RemoveAt(i);
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
