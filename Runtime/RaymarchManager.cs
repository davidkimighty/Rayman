using System.Collections.Generic;
using UnityEngine;

namespace Rayman
{
    [ExecuteInEditMode]
    public class RaymarchManager : MonoBehaviour
    {
        [SerializeField] private List<RaymarchRenderer> _raymarchRenderers;
        
#if UNITY_EDITOR
        [ContextMenu("Find All Renderers")]
        private void FindAllRenderers()
        {
            _raymarchRenderers = Utilities.GetObjectsByTypes<RaymarchRenderer>();
        }

        [ContextMenu("Reset All Shape Buffer")]
        private void ResetAllShapeBuffer()
        {
            foreach (RaymarchRenderer raymarchRenderer in _raymarchRenderers)
                raymarchRenderer.ResetShapeBuffer();
        }
#endif
    }
}
