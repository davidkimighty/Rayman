using UnityEngine;

namespace Rayman
{
    public enum DebugModes { None, Color, Normal, Hitmap, BoundingVolume, }
    
    [ExecuteInEditMode]
    public class RaymarchGroupDebugger : MonoBehaviour
    {
        private const string DebugModeKeyword = "DEBUG_MODE";
        private static readonly int DebugModeId = Shader.PropertyToID("_DebugMode");
        private static readonly int BoundsDisplayThresholdId = Shader.PropertyToID("_BoundsDisplayThreshold");

        [SerializeField] private RaymarchGroup raymarchGroup;
        [SerializeField] private DebugModes debugMode = DebugModes.Hitmap;
        [SerializeField] private int boundsDisplayThreshold = 300;

        private void OnEnable()
        {
            if (raymarchGroup == null) return;
            
            if (raymarchGroup.IsInitialized())
                Setup(raymarchGroup);
            else
                raymarchGroup.OnSetup += Setup;
        }

        private void OnDisable()
        {
            if (raymarchGroup == null) return;
            
            if (raymarchGroup.IsInitialized())
                raymarchGroup.MatInstance.DisableKeyword(DebugModeKeyword);
            raymarchGroup.OnSetup -= Setup;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (raymarchGroup == null)
                raymarchGroup = GetComponent<RaymarchGroup>();
            if (raymarchGroup == null) return;
            
            if (raymarchGroup.IsInitialized())
                SetupShaderProperties(ref raymarchGroup.MatInstance);
        }
        
        [ContextMenu("Find group")]
        public void FindGroup()
        {
            raymarchGroup = GetComponent<RaymarchGroup>();
        }
#endif

        public void Setup(RaymarchGroup group)
        {
            SetupShaderProperties(ref group.MatInstance);
        }
        
        private void SetupShaderProperties(ref Material material)
        {
            if (debugMode == DebugModes.None)
            {
                material.DisableKeyword(DebugModeKeyword);
                return;
            }
            
            material.EnableKeyword(DebugModeKeyword);
            material.SetInt(DebugModeId, (int)debugMode);
            material.SetInt(BoundsDisplayThresholdId, boundsDisplayThreshold);
        }
    }
}
