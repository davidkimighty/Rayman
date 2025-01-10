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
