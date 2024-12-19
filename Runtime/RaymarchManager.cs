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
#endif
    }
}
