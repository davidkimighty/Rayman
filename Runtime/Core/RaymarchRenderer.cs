using System;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Rendering;

namespace Rayman
{
#if UNITY_EDITOR
    public enum DebugModes { None, Color, Normal, Hitmap, BoundingVolume, }
#endif
    [ExecuteInEditMode]
    public class RaymarchRenderer : MonoBehaviour
    {
        public static readonly int MaxStepsId = Shader.PropertyToID("_MaxSteps");
        public static readonly int MaxDistanceId = Shader.PropertyToID("_MaxDistance");
        public static readonly int ShadowMaxStepsId = Shader.PropertyToID("_ShadowMaxSteps");
        public static readonly int ShadowMaxDistanceId = Shader.PropertyToID("_ShadowMaxDistance");
#if UNITY_EDITOR
        public static readonly int DebugModeId = Shader.PropertyToID("_DebugMode");
        public static readonly int BoundsDisplayThresholdId = Shader.PropertyToID("_BoundsDisplayThreshold");
#endif

        [SerializeField] protected Renderer mainRenderer;
        [SerializeField] protected int maxSteps = 64;
        [SerializeField] protected float maxDistance = 100f;
        [SerializeField] protected int shadowMaxSteps = 32;
        [SerializeField] protected float shadowMaxDistance = 30f;
        [SerializeField] protected List<RaymarchEntityGroup> raymarchGroups = new();
#if UNITY_EDITOR
        [Header("Debugging")]
        [SerializeField] protected bool executeInEditor;
        [SerializeField] protected DebugModes debugMode = DebugModes.None;
        [SerializeField] protected bool drawGizmos;
        [SerializeField] protected int boundsDisplayThreshold = 300;
#endif

        protected virtual void OnEnable()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying && !executeInEditor) return;
#endif
            if (Build())
            {
                RaymarchDebugger.Add(this);
                SpatialStructureDebugger.Add(this);
            }
        }
        
        protected virtual void LateUpdate()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying && !executeInEditor) return;
#endif
            if (!mainRenderer.isVisible) return;

            foreach (RaymarchEntityGroup group in raymarchGroups)
                group.SetData();
        }
        
        protected virtual void OnDisable()
        {
            Release();
        }
        
        [ContextMenu("Build")]
        public bool Build()
        {
            foreach (RaymarchEntityGroup group in raymarchGroups)
            {
                group.Setup();
                SetupRaymarchProperties(ref group.MaterialInstance);
#if UNITY_EDITOR
                if (debugMode != DebugModes.None)
                    SetupDebugProperties(ref group.MaterialInstance);
#endif
            }
            mainRenderer.materials = raymarchGroups.Select(g => g.MaterialInstance).ToArray();
            return true;
        }

        public void Release()
        {
            foreach (RaymarchEntityGroup group in raymarchGroups)
                group.Release();
            mainRenderer.materials = Array.Empty<Material>();
        }
        
        protected void SetupRaymarchProperties(ref Material mat)
        {
            if (mat == null) return;
            
            mat.SetInt(MaxStepsId, maxSteps);
            mat.SetFloat(MaxDistanceId, maxDistance);
            mat.SetInt(ShadowMaxStepsId, shadowMaxSteps);
            mat.SetFloat(ShadowMaxDistanceId, shadowMaxDistance);
        }
        
#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            if (mainRenderer == null)
                mainRenderer = GetComponent<Renderer>();
            //
            // if (executeInEditor && !IsInitialized)
            // {
            //     if (Build())
            //     {
            //         RaymarchDebugger.Add(this);
            //         SpatialStructureDebugger.Add(this);
            //     }
            // }
            //
            // if (!executeInEditor && !Application.isPlaying)
            // {
            //     SpatialStructureDebugger.Remove(this);
            //     RaymarchDebugger.Remove(this);
            //     Release();
            // }
            //
            // if (IsInitialized)
            // {
            //     for (int i = 0; i < groupData.Count; i++)
            //     {
            //         SetupRaymarchProperties(ref groupData[i].MaterialInstance);
            //         if (debugMode != DebugModes.None)
            //             SetupDebugProperties(ref groupData[i].MaterialInstance);
            //     }
            // }
        }

        protected void SetupDebugProperties(ref Material mat)
        {
            if (mat == null) return;
            
            mat.SetInt(DebugModeId, (int)debugMode);
            mat.SetInt(BoundsDisplayThresholdId, boundsDisplayThreshold);
        }
        
        [ContextMenu("Find all entities")]
        protected void FindAllEntities()
        {
            //raymarchEntities = RaymarchUtils.GetChildrenByHierarchical<RaymarchEntity>(transform);
            EditorUtility.SetDirty(this);
        }
#endif
    }
    
    
}