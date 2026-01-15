using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace Rayman
{
    [ExecuteInEditMode]
    public class ShapeGroupObject : MonoBehaviour
    {
        [SerializeField] private bool setupOnStart = true;
        [SerializeField] private Renderer mainRenderer;
        [SerializeField] private RayDataProvider rayDataProvider;
        [SerializeField] private Shader shader;
        [SerializeField] private List<MaterialDataProvider> materialDataProviders = new();
        [SerializeField] private List<ShapeGroup> shapeGroups;
#if UNITY_EDITOR
        [SerializeField] private bool drawGizmos;
#endif

        private Material material;
        private ShapeGroup[] groups;
        private ShapeProvider[] shapes;

        private BvhBufferProvider nodeBufferProvider;
        private GroupBufferProvider groupBufferProvider;
        private ShapeBufferProvider<ShapeGroupData> shapeBufferProvider;
        private ColorBufferProvider<ColorData> colorBufferProvider;

        private NativeArray<Aabb> leafBounds;
        private NativeArray<int> primitiveIds;

        public bool IsInitialized => material && nodeBufferProvider != null;
        public Material Material => material;

        private void Start()
        {
            if (Application.isPlaying && setupOnStart)
                SetupMaterial();
        }

        private void LateUpdate()
        {
            if (!IsInitialized) return;

            UpdateBufferData();

            // isDirty checks?
            nodeBufferProvider.SetData(leafBounds);
            shapeBufferProvider.SetData(shapes);
            colorBufferProvider.SetData(shapes);
            groupBufferProvider.SetData(groups);
        }

        private void OnDestroy()
        {
            CleanupMaterial();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!IsInitialized) return;

            rayDataProvider?.ProvideData(ref material);
            foreach (MaterialDataProvider provider in materialDataProviders)
                provider?.ProvideData(ref material);
        }

        void OnDrawGizmos()
        {
            if (!drawGizmos || !IsInitialized) return;

            nodeBufferProvider.DrawGizmos();
        }
#endif

        [ContextMenu("Setup Material")]
        public void SetupMaterial()
        {
            if (shapeGroups == null || shapeGroups.Count == 0) return;

            if (IsInitialized)
                CleanupMaterial();

            material = new Material(shader);
            rayDataProvider?.ProvideData(ref material);
            foreach (MaterialDataProvider provider in materialDataProviders)
                provider?.ProvideData(ref material);
            material.EnableKeyword("_SHAPE_GROUP");

            SetupBufferData();

            int shapeCount = shapes.Length;
            leafBounds = new NativeArray<Aabb>(shapeCount, Allocator.Persistent);
            primitiveIds = new NativeArray<int>(shapeCount, Allocator.Persistent);

            UpdateBufferData();

            for (int i = 0; i < shapeCount; i++)
                primitiveIds[i] = i;

            nodeBufferProvider = new BvhBufferProvider();
            nodeBufferProvider.InitializeBuffer(ref material, leafBounds, primitiveIds);

            shapeBufferProvider = new ShapeBufferProvider<ShapeGroupData>();
            shapeBufferProvider.InitializeBuffer(ref material, shapes);

            colorBufferProvider = new ColorBufferProvider<ColorData>();
            colorBufferProvider.InitializeBuffer(ref material, shapes);

            groupBufferProvider = new GroupBufferProvider();
            groupBufferProvider.InitializeBuffer(ref material, groups);

            mainRenderer.material = material;
        }

        [ContextMenu("Cleanup Material")]
        public void CleanupMaterial()
        {
            nodeBufferProvider?.ReleaseBuffer();
            shapeBufferProvider?.ReleaseBuffer();
            colorBufferProvider?.ReleaseBuffer();
            groupBufferProvider?.ReleaseBuffer();

            if (leafBounds.IsCreated) leafBounds.Dispose();
            if (primitiveIds.IsCreated) primitiveIds.Dispose();

            if (Application.isEditor)
                DestroyImmediate(material);
            else
                Destroy(material);

            if (mainRenderer)
                mainRenderer.materials = Array.Empty<Material>();
        }

        private void SetupBufferData()
        {
            List<ShapeGroup> groupList = new();
            List<ShapeProvider> shapeList = new();

            for (int i = 0; i < shapeGroups.Count; i++)
            {
                ShapeGroup group = shapeGroups[i];
                groupList.Add(group);

                foreach (ShapeProvider provider in group.Shapes)
                    shapeList.Add(provider);
            }
            
            groups = groupList.ToArray();
            shapes = shapeList.ToArray();
        }

        private void UpdateBufferData()
        {
            for (int i = 0; i < shapes.Length; i++)
            {
                ShapeProvider provider = shapes[i];
                if (!provider) continue;

                leafBounds[i] = provider.GetBounds();
            }
        }
    }
}
