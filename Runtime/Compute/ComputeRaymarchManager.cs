using System.Collections.Generic;
using UnityEngine;

namespace Rayman
{
    public class ComputeRaymarchManager : MonoBehaviour
    {
        [SerializeField] private ComputeRaymarchFeature raymarchFeature;
        [SerializeField] private List<ShapeElement> Shapes = new();
        [SerializeField] private bool buildOnStart;
        [SerializeField] private float boundsUpdateThreshold;
        [SerializeField] private bool drawGizmos;
        
        private ISpatialStructure<Aabb> bvh;
        private ColorShapeData[] shapeData;
        private AabbNodeData[] nodeData;

        public ColorShapeData[] ShapeData => shapeData;
        public AabbNodeData[] NodeData => nodeData;

        private void Start()
        {
            if (raymarchFeature == null) return;
            
            if (!raymarchFeature.isActive)
                raymarchFeature.SetActive(true);
            
            if (buildOnStart)
            {
                Build();
            }
        }

        private void LateUpdate()
        {
            //if (!IsInitialized) return;
            
            // for (int j = 0; j < boundingVolumes.Length; j++)
            // {
            //     BoundingVolume<Aabb> volume = boundingVolumes[j];
            //     volume.SyncVolume(ref bvh, boundsUpdateThreshold);
            //     
            //     var shape = volume.Source as RaymarchShapeElement;
            //     if (shape == null) continue;
            //     
            //     shapeData[j] = new ColorShapeData();
            //     shapeData[j].InitializeData(shape);
            // }
            UpdateNodeData(bvh, ref nodeData);
        }

        [ContextMenu("Build")]
        public void Build()
        {
            if (Shapes.Count == 0) return;
            
            //List<BoundingVolume<Aabb>> volumes = new();
            foreach (ShapeElement shape in Shapes)
            {
                if (shape == null || !shape.gameObject.activeInHierarchy) continue;
                
                //volumes.Add(new BoundingVolume<Aabb>(shape));
            }
            //if (volumes.Count == 0) return;
            
            //boundingVolumes = volumes.ToArray();
            //bvh = Bvh<Aabb>.Create(boundingVolumes);
            
            //int shapeCount = boundingVolumes.Length;
            //shapeData = new ColorShapeData[shapeCount];
            
            int nodesCount = SpatialNode<Aabb>.GetNodesCount(bvh.Root);
            nodeData = new AabbNodeData[nodesCount];

            raymarchFeature.RaymarchManager = this;
        }
        
        private void UpdateNodeData(ISpatialStructure<Aabb> structure, ref AabbNodeData[] nodeData)
        {
            int index = 0;
            Queue<(SpatialNode<Aabb> node, int parentIndex)> queue = new();
            queue.Enqueue((structure.Root, -1));

            while (queue.Count > 0)
            {
                (SpatialNode<Aabb> current, int parentIndex) = queue.Dequeue();
                AabbNodeData data = new()
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
        }
        
        private void OnDrawGizmos()
        {
            if (bvh == null || !drawGizmos) return;

            bvh.DrawStructure();
        }

        [ContextMenu("Find All Shapes")]
        private void FindAllGroups()
        {
            Shapes = RaymarchUtils.GetChildrenByHierarchical<ShapeElement>();
        }
#endif
    }
}
