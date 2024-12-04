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
        [SerializeField] private RaymarchFeature raymarchFeature;
        [SerializeField] private List<ComputeRaymarchRenderer> renderers;
#if UNITY_EDITOR
        [SerializeField] private bool enableRaymarchDebug;
#endif

        private void Awake()
        {
            if (Application.isPlaying)
            {
                if (!raymarchFeature.isActive)
                    raymarchFeature.SetActive(true);
                raymarchFeature?.ClearAndRegister(renderers);
            }
        }

#if UNITY_EDITOR
        private void OnGUI()
        {
            if (!raymarchFeature.isActive)
                raymarchFeature.SetActive(true);
            
            if (enableRaymarchDebug)
                AddDefineSymbol(RaymarchFeature.DebugKeyword);
            else
                RemoveDefineSymbol(RaymarchFeature.DebugKeyword);
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
            renderers = Utilities.GetObjectsByTypes<ComputeRaymarchRenderer>();
        }

        [ContextMenu("Register Renderers")]
        private void RegisterRenderers()
        {
            if (renderers.Count == 0) return;
            
            raymarchFeature?.ClearAndRegister(renderers);
        }
#endif
    }
}
