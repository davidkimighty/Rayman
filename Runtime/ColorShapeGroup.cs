using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Rayman
{
    [ExecuteInEditMode]
    public class ColorShapeGroup : RaymarchGroup
    {
        protected static readonly int SrcBlendId = Shader.PropertyToID("_SrcBlend");
        protected static readonly int DstBlendId = Shader.PropertyToID("_DstBlend");
        protected static readonly int CullId = Shader.PropertyToID("_Cull");
        protected static readonly int ZWriteId = Shader.PropertyToID("_ZWrite");
        
        protected static readonly int MetallicId = Shader.PropertyToID("_Metallic");
        protected static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        
        protected static readonly int NodeBufferId = Shader.PropertyToID("_NodeBuffer");
        protected static readonly int ShapeBufferId = Shader.PropertyToID("_ShapeBuffer");
        
        [SerializeField] protected List<ColorShape> entities = new();
        [SerializeField] protected float updateBoundsThreshold;
        
        [Header("Shader Properties")]
        [SerializeField] protected RenderStateData renderStateData;
        [Range(0f, 1f), SerializeField] protected float metallic;
        [Range(0f, 1f), SerializeField] protected float smoothness = 0.5f;
        [ColorUsage(true, true), SerializeField] protected Color emissionColor;
        
        protected ColorShape[] activeEntities;
        protected ISpatialStructure<Aabb> bvh;
        protected BoundingVolume<Aabb>[] boundingVolumes;
        
        protected NodeDataAabb[] nodeData;
        protected GraphicsBuffer nodeBuffer;
        protected ShapeColorData[] shapeData;
        protected GraphicsBuffer shapeBuffer;

        private void LateUpdate()
        {
            if (!IsInitialized()) return;

            for (int i = 0; i < boundingVolumes.Length; i++)
                boundingVolumes[i].SyncVolume(ref bvh, updateBoundsThreshold);
            
            UpdateNodeData();
            nodeBuffer.SetData(nodeData);
            
            UpdateShapeData();
            shapeBuffer.SetData(shapeData);
        }
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!IsInitialized()) return;

            if (MatInstance)
            {
                SetupShaderProperties(ref MatInstance);                
            }
        }

        private void OnDrawGizmos()
        {
            if (!drawGizmos || !IsInitialized()) return;
            
            bvh.DrawStructure();
        }
#endif

        public override Material InitializeGroup()
        {
            activeEntities = entities.Where(s => s && s.gameObject.activeInHierarchy).ToArray();
            if (activeEntities.Length == 0) return null;

            MatInstance = new Material(shader);
            if (!MatInstance) return null;
            
            SetupShaderProperties(ref MatInstance);

            SetupNodeBuffer(ref MatInstance);
            SetupShapeBuffer(ref MatInstance);
            
            InvokeOnSetup();
            return MatInstance;
        }

        public override void ReleaseGroup()
        {
            if (Application.isEditor)
                DestroyImmediate(MatInstance);
            else
                Destroy(MatInstance);
            activeEntities = null;
            ReleaseNodeBuffer();
            ReleaseShapeBuffer();
            
            InvokeOnRelease();
        }
        
        public override bool IsInitialized() => MatInstance && activeEntities != null &&
                                                bvh != null && boundingVolumes != null;
        
        public override void SetupShaderProperties(ref Material material)
        {
            if (renderStateData != null)
            {
                material.SetFloat(SrcBlendId, (float)renderStateData.SrcBlend);
                material.SetFloat(DstBlendId, (float)renderStateData.DstBlend);
                material.SetInt(CullId, (int)renderStateData.Cull);
                material.SetFloat(ZWriteId, renderStateData.ZWrite ? 1f : 0f);
            }
            
            material.SetFloat(MetallicId, metallic);
            material.SetFloat(SmoothnessId, smoothness);
            material.SetColor(EmissionColorId, emissionColor);
        }
        
#if UNITY_EDITOR
        [ContextMenu("Find All Shapes")]
        public void FindAllShapes()
        {
            entities = RaymarchUtils.GetChildrenByHierarchical<ColorShape>(transform);
        }
#endif
        
        private void UpdateNodeData()
        {
            int index = 0;
            Queue<(SpatialNode<Aabb> node, int parentIndex)> queue = new();
            queue.Enqueue((bvh.Root, -1));

            while (queue.Count > 0)
            {
                (SpatialNode<Aabb> current, int parentIndex) = queue.Dequeue();
                NodeDataAabb node = new()
                {
                    Id = current.Id,
                    ChildIndex = -1,
                    Bounds = current.Bounds,
                };

                if (current.LeftChild != null)
                {
                    node.ChildIndex = index + queue.Count + 1;
                    queue.Enqueue((current.LeftChild, index));
                }
                if (current.RightChild != null)
                    queue.Enqueue((current.RightChild, index));
                
                nodeData[index] = node;
                index++;
            }
        }

        private void UpdateShapeData()
        {
            for (int i = 0; i < activeEntities.Length; i++)
            {
                if (!activeEntities[i]) continue;

                shapeData[i] = new ShapeColorData(activeEntities[i]);
            }
        }
        
        private void SetupNodeBuffer(ref Material mat)
        {
            nodeBuffer?.Release();

            boundingVolumes = activeEntities.Select(e => new BoundingVolume<Aabb>(e)).ToArray();
            bvh = Bvh<Aabb>.Create(boundingVolumes);
            int nodeCount = bvh.Count;
            if (nodeCount == 0) return;

            nodeData = new NodeDataAabb[nodeCount];
            nodeBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, nodeCount, NodeDataAabb.Stride);
            mat.SetBuffer(NodeBufferId, nodeBuffer);
        }

        private void SetupShapeBuffer(ref Material mat)
        {
            shapeBuffer?.Release();
            int count = activeEntities.Length;
            if (count == 0) return;

            shapeData = new ShapeColorData[count];
            shapeBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, count, ShapeColorData.Stride);
            mat.SetBuffer(ShapeBufferId, shapeBuffer);
        }

        private void ReleaseNodeBuffer()
        {
            bvh = null;
            boundingVolumes = null;
            nodeBuffer?.Release();
            nodeData = null;
        }

        private void ReleaseShapeBuffer()
        {
            shapeBuffer?.Release();
            shapeData = null;
        }
    }
}
