using System.Collections.Generic;
using UnityEngine;

namespace Rayman
{
    [ExecuteInEditMode]
    public class ShapeGroupObject : RaymarchObject
    {
        [SerializeField] private BufferProvider<ShapeProvider> shapeBufferProvider;
        [SerializeField] private BufferProvider<IRaymarchGroup> groupBufferProvider;
        [SerializeField] private BufferProvider<IBoundsProvider> shapeNodeBufferProvider;
        [SerializeField] private BufferProvider<VisualProvider> visualBufferProvider;
        [SerializeField] private List<ShapeGroup> shapeGroups;

        private ShapeGroup[] groupProviders;
        private ShapeProvider[] shapeProviders;
        private VisualProvider[] visualProviders;

        private void LateUpdate()
        {
            if (!material) return;

            shapeBufferProvider?.SetData();
            groupBufferProvider?.SetData();
            shapeNodeBufferProvider?.SetData();
            visualBufferProvider?.SetData();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!material) return;

            SetMaterialData();
        }
#endif

        public override Material CreateMaterial()
        {
            if (material)
                Cleanup();
            material = new Material(shader);

            SetMaterialData();
            SetupDataProviders();

            shapeBufferProvider?.InitializeBuffer(ref material, shapeProviders);
            groupBufferProvider?.InitializeBuffer(ref material, groupProviders);
            shapeNodeBufferProvider?.InitializeBuffer(ref material, shapeProviders);
            visualBufferProvider?.InitializeBuffer(ref material, visualProviders);

            return material;
        }

        public override void Cleanup()
        {
            shapeBufferProvider?.ReleaseBuffer();
            groupBufferProvider?.ReleaseBuffer();
            shapeNodeBufferProvider?.ReleaseBuffer();
            visualBufferProvider?.ReleaseBuffer();

            if (Application.isEditor)
                DestroyImmediate(material);
            else
                Destroy(material);
        }
        
#if UNITY_EDITOR
        [ContextMenu("Find All BufferProviders")]
        public void FindAllBufferProviders()
        {
            shapeBufferProvider = GetComponent<BufferProvider<ShapeProvider>>();
            groupBufferProvider = GetComponent<BufferProvider<IRaymarchGroup>>();
            shapeNodeBufferProvider = GetComponent<BufferProvider<IBoundsProvider>>();
            visualBufferProvider = GetComponent<BufferProvider<VisualProvider>>();
        }
#endif

        protected override void SetMaterialData()
        {
            base.SetMaterialData();
            material.EnableKeyword("_SHAPE_GROUP");
        }

        private void SetupDataProviders()
        {
            List<ShapeGroup> groupList = new();
            List<ShapeProvider> shapeList = new();
            List<VisualProvider> visualList = new();

            for (int i = 0; i < shapeGroups.Count; i++)
            {
                ShapeGroup group = shapeGroups[i];
                if (!group) continue;

                groupList.Add(group);
                foreach (ShapeVisual item in group.Items)
                {
                    if (item.ShapeProvider)
                    {
                        item.ShapeProvider.GroupIndex = i;
                        shapeList.Add(item.ShapeProvider);
                    }
                    if (item.VisualProvider)
                        visualList.Add(item.VisualProvider);
                }
            }
            groupProviders = groupList.ToArray();
            shapeProviders = shapeList.ToArray();
            visualProviders = visualList.ToArray();
        }
    }
}
