using System.Collections.Generic;
using UnityEngine;

namespace Rayman
{
    [ExecuteInEditMode]
    public class RaymarchManager : MonoBehaviour
    {
        [SerializeField] private List<RaymarchRenderer> raymarchRenderers;
        
#if UNITY_EDITOR
        [ContextMenu("Find All Renderers")]
        private void FindAllRenderers()
        {
            raymarchRenderers = Utilities.GetChildrenByHierarchical<RaymarchRenderer>();
        }

        [ContextMenu("Reset All Shape Buffer")]
        private void ResetAllShapeBuffer()
        {
            foreach (RaymarchRenderer raymarchRenderer in raymarchRenderers)
                raymarchRenderer.ResetShapeBuffer();
        }
#endif
    }
}
