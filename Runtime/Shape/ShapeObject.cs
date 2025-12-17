using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Rayman
{
    [ExecuteInEditMode]
    public class ShapeObject : RaymarchObject
    {
        [SerializeField] private BufferProvider<ShapeProvider> shapeBufferProvider;
        [SerializeField] private BufferProvider<IBoundsProvider> shapeNodeBufferProvider;
        [SerializeField] private BufferProvider<VisualProvider> visualBufferProvider;
        [SerializeField] private List<GameObject> shapes;

        private ShapeProvider[] shapeProviders;
        private VisualProvider[] visualProviders;

        private void LateUpdate()
        {
            if (!material) return;

            shapeBufferProvider?.SetData();
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
            shapeNodeBufferProvider?.InitializeBuffer(ref material, shapeProviders);
            visualBufferProvider?.InitializeBuffer(ref material, visualProviders);

            return material;
        }

        public override void Cleanup()
        {
            shapeBufferProvider?.ReleaseBuffer();
            shapeNodeBufferProvider?.ReleaseBuffer();
            visualBufferProvider?.ReleaseBuffer();

            if (Application.isEditor)
                DestroyImmediate(material);
            else
                Destroy(material);
        }

        public override void Refresh()
        {
            if (!material) return;
            
            shapeBufferProvider?.ReleaseBuffer();
            shapeNodeBufferProvider?.ReleaseBuffer();
            visualBufferProvider?.ReleaseBuffer();
            
            SetMaterialData();
            SetupDataProviders();
            
            shapeBufferProvider?.InitializeBuffer(ref material, shapeProviders);
            shapeNodeBufferProvider?.InitializeBuffer(ref material, shapeProviders);
            visualBufferProvider?.InitializeBuffer(ref material, visualProviders);
            
            shapeBufferProvider?.SetData();
            shapeNodeBufferProvider?.SetData();
            visualBufferProvider?.SetData();
        }

        public void AddShape(GameObject shape)
        {
            shapes.Add(shape);
        }
        
#if UNITY_EDITOR
        [ContextMenu("Find All BufferProviders")]
        public void FindAllBufferProviders()
        {
            shapeBufferProvider = GetComponent<BufferProvider<ShapeProvider>>();
            shapeNodeBufferProvider = GetComponent<BufferProvider<IBoundsProvider>>();
            visualBufferProvider = GetComponent<BufferProvider<VisualProvider>>();
        }
        
        [ContextMenu("Add Default BufferProviders")]
        public void AddDefaultBufferProviders()
        {
            shapeBufferProvider = gameObject.AddComponent<ShapeBufferProvider>();
            shapeNodeBufferProvider = gameObject.AddComponent<BvhAabbBufferProvider>();
            visualBufferProvider = gameObject.AddComponent<ColorBufferProvider>();
            EditorUtility.SetDirty(this);
        }
#endif

        private void SetupDataProviders()
        {
            List<ShapeProvider> shapeList = new();
            List<VisualProvider> visualList = new();

            foreach (GameObject shape in shapes)
            {
                if (shape.TryGetComponent(out ShapeProvider shapeProvider))
                    shapeList.Add(shapeProvider);
                if (shape.TryGetComponent(out VisualProvider visualProvider))
                    visualList.Add(visualProvider);
            }
            shapeProviders = shapeList.ToArray();
            visualProviders = visualList.ToArray();
        }
    }
}
