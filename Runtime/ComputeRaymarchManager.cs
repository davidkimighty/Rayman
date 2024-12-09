using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
#endif
using UnityEngine;

namespace Rayman
{
    [ExecuteInEditMode]
    public class ComputeRaymarchManager : MonoBehaviour
    {
        [Serializable]
        public struct Setting
        {
            public float BoundsBuffSize;
#if UNITY_EDITOR
            public bool EnableRaymarchDebug;
            public bool ShowHitmap;
            public bool DrawBoundingVolumes;
#endif
        }
        
        [SerializeField] private Setting setting;
        [SerializeField] private RaymarchFeature raymarchFeature;
        [SerializeField] private List<ComputeRaymarchRenderer> raymarchRenderers = new();

        private ISpatialStructure<AABB> bvh;
        private BoundingVolume<AABB>[] boundingVolumes;

        private void Awake()
        {
            if (!raymarchFeature.isActive)
                raymarchFeature.SetActive(true);

            bvh = new BVH<AABB>();
            boundingVolumes = GetActiveBoundingVolumes();
            bvh.Build(boundingVolumes);
            
            raymarchFeature.ClearAndRegister(raymarchRenderers);
#if RAYMARCH_DEBUG
            raymarchFeature.SetHitMap(setting.ShowHitmap);
#endif
        }

        private void Update()
        {
            SyncBoundingVolumes();
        }

        private BoundingVolume<AABB>[] GetActiveBoundingVolumes()
        {
            List<BoundingVolume<AABB>> bounds = new();
            foreach (ComputeRaymarchRenderer raymarchRenderer in raymarchRenderers)
            {
                if (raymarchRenderer == null) continue;
                
                foreach (RaymarchShape shape in raymarchRenderer.Shapes)
                {
                    if (shape == null) continue;
                    
                    bounds.Add(new BoundingVolume<AABB>
                    {
                        Source = shape,
                        Bounds = shape.GetBounds<AABB>()
                    });
                }
            }
            return bounds.ToArray();
        }

        private void SyncBoundingVolumes()
        {
            if (boundingVolumes == null) return;

            for (int i = 0; i < boundingVolumes.Length; i++)
            {
                BoundingVolume<AABB> bv = boundingVolumes[i];
                if (bv.Source == null) continue;
                
                AABB buffBounds = bv.Bounds.Expand(setting.BoundsBuffSize);
                AABB newBounds = bv.Source.GetBounds<AABB>();
                if (buffBounds.Contains(newBounds)) continue;
                
                bv.Bounds = newBounds;
                bvh.UpdateBounds(i, newBounds);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (setting.EnableRaymarchDebug)
                AddDefineSymbol(RaymarchFeature.DebugKeyword);
            else
                RemoveDefineSymbol(RaymarchFeature.DebugKeyword);
        }
        
        private void OnDrawGizmos()
        {
            if (bvh == null || !setting.DrawBoundingVolumes) return;
            
            bvh.DrawStructure();
        }

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
        
        [ContextMenu("Find All Renderers")]
        private void FindAllRenderers()
        {
            raymarchRenderers = Utilities.GetObjectsByTypes<ComputeRaymarchRenderer>();
        }

        [ContextMenu("Register Renderers")]
        private void RegisterRenderers()
        {
            if (raymarchRenderers.Count == 0) return;
            
            raymarchFeature?.ClearAndRegister(raymarchRenderers);
        }
#endif
    }
}
