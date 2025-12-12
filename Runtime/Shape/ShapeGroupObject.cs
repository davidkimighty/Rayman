using System.Collections.Generic;
using UnityEngine;

namespace Rayman
{
    [ExecuteInEditMode]
    public class ShapeGroupObject : RaymarchObject
    {
        [SerializeField] private BufferProvider<IRaymarchGroup> groupBufferProvider;
        [SerializeField] private BufferProvider<ShapeProvider> shapeBufferProvider;
        [SerializeField] private BufferProvider<IBoundsProvider> groupNodeBufferProvider;
        [SerializeField] private BufferProvider<IBoundsProvider> shapeNodeBufferProvider;
        [SerializeField] private BufferProvider<VisualProvider> visualBufferProvider;
        [SerializeField] private List<ShapeGroup> shapeGroups;

        private ShapeGroup[] groupProviders;
        private ShapeProvider[] shapeProviders;
        private VisualProvider[] visualProviders;

        private void LateUpdate()
        {
            if (!material) return;

            groupBufferProvider?.SetData();
            shapeBufferProvider?.SetData();
            groupNodeBufferProvider?.SetData();
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

            groupBufferProvider?.InitializeBuffer(ref material, groupProviders);
            shapeBufferProvider?.InitializeBuffer(ref material, shapeProviders);
            groupNodeBufferProvider?.InitializeBuffer(ref material, groupProviders);
            shapeNodeBufferProvider?.InitializeBuffer(ref material, shapeProviders);
            visualBufferProvider?.InitializeBuffer(ref material, visualProviders);

            return material;
        }

        public override void Cleanup()
        {
            groupBufferProvider?.ReleaseBuffer();
            shapeBufferProvider?.ReleaseBuffer();
            groupNodeBufferProvider?.ReleaseBuffer();
            shapeNodeBufferProvider?.ReleaseBuffer();
            visualBufferProvider?.ReleaseBuffer();

            if (Application.isEditor)
                DestroyImmediate(material);
            else
                Destroy(material);
        }

        private void SetupDataProviders()
        {
            List<ShapeGroup> groupList = new();
            List<ShapeProvider> shapeList = new();
            List<VisualProvider> visualList = new();

            foreach (ShapeGroup group in shapeGroups)
            {
                if (group == null) continue;

                groupList.Add(group);
                foreach (ShapeVisual item in group.Items)
                {
                    if (item.ShapeProvider != null) shapeList.Add(item.ShapeProvider);
                    if (item.VisualProvider != null) visualList.Add(item.VisualProvider);
                }
            }

            groupProviders = groupList.ToArray();
            shapeProviders = shapeList.ToArray();
            visualProviders = visualList.ToArray();
        }
    }
}
