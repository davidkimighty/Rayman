using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Rayman
{
    [ExecuteInEditMode]
    public class ComputeRaymarchManager : MonoBehaviour, IComputeRaymarchDataProvider
    {
        [SerializeField] private ComputeRaymarchFeature raymarchFeature;
        [SerializeField] private bool buildOnAwake;
        [SerializeField] private float boundsExpandSize;
        [SerializeField] private List<ComputeRaymarchRenderer> raymarchRenderers = new();
#if UNITY_EDITOR
        [SerializeField] private DebugModes debugMode = DebugModes.None;
        [SerializeField] private bool drawGizmos;
        [SerializeField] private bool showLabel;
        [SerializeField] private int boundsDisplayThreshold = 1300;
#endif
        private ISpatialStructure<AABB> bvh;
        private BoundingVolume<AABB>[] boundingVolumes;
        private ShapeData[] shapeData;
        private DistortionData[] distortionData;
        private NodeData<AABB>[] nodeData;

        public ISpatialStructure<AABB> SpatialStructure => bvh;

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

        private void Update()
        {
            if (boundingVolumes == null) return;
            
            RaymarchRenderer.SyncBoundingVolumes<AABB>(ref bvh, ref boundingVolumes);
            RaymarchRenderer.UpdateShapeData<AABB>(boundingVolumes, ref shapeData);
            RaymarchRenderer.UpdateOperationData<AABB>(boundingVolumes, ref distortionData);
            RaymarchRenderer.FillNodeData<AABB>(bvh, ref nodeData);
        }

        public ShapeData[] GetShapeData() => shapeData;

        public DistortionData[] GetDistortionData() => distortionData;

        public NodeData<AABB>[] GetNodeData() => nodeData;

        [ContextMenu("Build")]
        public void Build()
        {
            if (raymarchRenderers.Count == 0) return;
            
            List<BoundingVolume<AABB>> volumes = new();
            foreach (ComputeRaymarchRenderer rr in raymarchRenderers)
            {
                if (rr == null || !rr.gameObject.activeInHierarchy) continue;
                
                volumes.AddRange(rr.CreateBoundingVolumes<AABB>());
            }
            boundingVolumes = volumes.ToArray();
            bvh = RaymarchRenderer.CreateSpatialStructure<AABB>(boundingVolumes);
#if UNITY_EDITOR
            SpatialStructureDebugger.Add(bvh);
#endif
            
            int shapeCount = boundingVolumes.Length;
            shapeData = new ShapeData[shapeCount];
            
            int distortionCount = boundingVolumes.Count(v => v.Source.Settings.Distortion.Enabled);
            distortionData = new DistortionData[distortionCount];
            
            int nodesCount = SpatialNode<AABB>.GetNodesCount(bvh.Root);
            nodeData = new NodeData<AABB>[nodesCount];
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
                raymarchFeature = Utilities.GetRendererFeature<ComputeRaymarchFeature>();
            else
            {
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

        [ContextMenu("Find All Raymarch Renderers")]
        private void FindAllGroups()
        {
            raymarchRenderers = Utilities.GetChildrenByHierarchical<ComputeRaymarchRenderer>();
        }
#endif
    }
}
