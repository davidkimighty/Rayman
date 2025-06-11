using UnityEngine;

namespace Rayman
{
    public enum DebugModes { None, Normal, Hitmap, BoundingVolume, }
    
    [ExecuteInEditMode]
    public class RaymarchVisualDebugger : MonoBehaviour
    {
        private const string DebugModeKeyword = "DEBUG_MODE";
        private static readonly int DebugModeId = Shader.PropertyToID("_DebugMode");
        private static readonly int BoundsDisplayThresholdId = Shader.PropertyToID("_BoundsDisplayThreshold");

        [SerializeField] private RaymarchObject raymarchObject;
        [SerializeField] private DebugModes debugMode = DebugModes.Hitmap;
        [SerializeField] private int boundsDisplayThreshold = 300;

        private void OnEnable()
        {
            if (raymarchObject == null) return;
            
            if (raymarchObject.IsReady())
                Setup(raymarchObject);
            else
                raymarchObject.OnSetup += Setup;
        }

        private void OnDisable()
        {
            if (raymarchObject == null) return;
            
            if (raymarchObject.IsReady())
                raymarchObject.MatInstance.DisableKeyword(DebugModeKeyword);
            raymarchObject.OnSetup -= Setup;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (raymarchObject == null)
                raymarchObject = GetComponent<RaymarchObject>();
            if (raymarchObject == null) return;
            
            if (raymarchObject.IsReady())
                SetupShaderProperties(ref raymarchObject.MatInstance);
        }
        
        [ContextMenu("Find object")]
        public void FindObject()
        {
            raymarchObject = GetComponent<RaymarchObject>();
        }
#endif

        public void Setup(RaymarchObject group)
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
