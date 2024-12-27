using System.Collections.Generic;
using System.Reflection;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
#endif
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Rayman
{
    public static class RaymarchUtils
    {
        public static List<BoundingVolume<T>> CreateBoundingVolumes<T>(List<RaymarchShape> shapes) where T : struct, IBounds<T>
        {
            var volumes = new List<BoundingVolume<T>>();
            foreach (RaymarchShape shape in shapes)
            {
                T bounds = shape.GetBounds<T>();
                volumes.Add(new BoundingVolume<T>(shape, bounds));
            }
            return volumes;
        }
        
        public static BVH<T> CreateSpatialStructure<T>(BoundingVolume<T>[] volumes) where T : struct, IBounds<T>
        {
            BVH<T> spatialStructure = new();
            int shapeId = 0;
            
            for (int i = 0; i < volumes.Length; i++)
            {
                BoundingVolume<T> volume = volumes[i];
                spatialStructure.AddLeafNode(shapeId, volume.Bounds, volume.Source);
                shapeId++;
            }
            return spatialStructure;
        }

        public static void SyncBoundingVolumes<T>(ref ISpatialStructure<T> spatialStructure,
            ref BoundingVolume<T>[] boundingVolumes) where T : struct, IBounds<T>
        {
            for (int i = 0; i < boundingVolumes.Length; i++)
            {
                BoundingVolume<T> volume = boundingVolumes[i];
                T buffBounds = volume.Bounds.Expand(volume.Source.Settings.ExtraMoveBounds);
                T newBounds = volume.Source.GetBounds<T>();
                if (buffBounds.Contains(newBounds)) continue;

                volume.Bounds = newBounds;
                spatialStructure.UpdateBounds(volume.Source, newBounds);
            }
        }

        public static void UpdateShapeData<T>(BoundingVolume<T>[] boundingVolumes,
            ref ShapeData[] shapeData) where T : struct, IBounds<T>
        {
            for (int i = 0; i < shapeData.Length; i++)
            {
                RaymarchShape shape = boundingVolumes[i].Source;
                if (shape == null) continue;
                
                shapeData[i] = new ShapeData(shape.transform, shape.Settings);
            }
        }

        public static void FillNodeData<T>(ISpatialStructure<T> spatialStructure,
            ref NodeData<T>[] nodeData) where T : struct, IBounds<T>
        {
            int index = 0;
            Queue<(SpatialNode<T> node, int parentIndex)> queue = new();
            queue.Enqueue((spatialStructure.Root, -1));

            while (queue.Count > 0)
            {
                (SpatialNode<T> current, int parentIndex) = queue.Dequeue();
                NodeData<T> data = new()
                {
                    Id = current.Id,
                    Bounds = current.Bounds,
                    Parent = parentIndex,
                    Left = -1,
                    Right = -1,
                };

                if (current.LeftChild != null)
                {
                    queue.Enqueue((current.LeftChild, index));
                    data.Left = index + queue.Count;
                }
                if (current.RightChild != null)
                {
                    queue.Enqueue((current.RightChild, index));
                    data.Right = index + queue.Count;
                }
                nodeData[index] = data;
                index++;
            }
        }
        
        public static List<T> GetChildrenByHierarchical<T>(Transform root = null) where T : Component
        {
            List<T> found = new();
            Transform[] transforms = Object.FindObjectsByType<Transform>(FindObjectsSortMode.InstanceID);

            foreach (Transform transform in transforms)
            {
                if (transform.parent != root) continue;
                
                SearchAdd(transform);
            }
            return found;
            
            void SearchAdd(Transform target)
            {
                if (!target.gameObject.activeInHierarchy) return;
                
                T component = target.GetComponent<T>();
                if (component != null)
                    found.Add(component);

                foreach (Transform child in target)
                    SearchAdd(child);
            }
        }

        public static T GetRendererFeature<T>() where T : ScriptableRendererFeature
        {
            var renderPipeline = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
            if (renderPipeline == null)
            {
                Debug.LogError("Universal Render Pipeline not found.");
                return null;
            }

            ScriptableRenderer scriptableRenderer = renderPipeline.GetRenderer(0);
            PropertyInfo property = typeof(ScriptableRenderer).GetProperty("rendererFeatures",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var features = property.GetValue(scriptableRenderer) as List<ScriptableRendererFeature>;

            T rendererFeature = null;
            foreach (var feature in features)
            {
                if (feature.GetType() == typeof(T))
                {
                    rendererFeature = feature as T;
                    break;
                }
            }
            return rendererFeature;
        }
        
#if UNITY_EDITOR
        public static void AddDefineSymbol(string symbol)
        {
            string currentSymbols = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone);
            if (!currentSymbols.Contains(symbol))
                PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Standalone, symbol);
        }

        public static void RemoveDefineSymbol(string symbol)
        {
            string currentSymbols = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone);
            if (currentSymbols.Contains(symbol))
            {
                PlayerSettings.SetScriptingDefineSymbols(
                    NamedBuildTarget.Standalone, 
                    currentSymbols.Replace(symbol + ";", "").Replace(symbol, "")
                );
            }
        }
#endif
    }
}
