using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rayman
{
    public class RaymarchManager : MonoBehaviour
    {
        public event Action<RaymarchRenderer> OnAddRenderer;
        public event Action<RaymarchRenderer> OnRemoveRenderer;
        
        [SerializeField] protected List<RaymarchRenderer> raymarchRenderers = new();

        public List<RaymarchRenderer> Renderers => raymarchRenderers;

#if UNITY_EDITOR
        [ContextMenu("Find all Raymarch Renderers")]
        public void FindAllRaymarchRenderers()
        {
            raymarchRenderers = RaymarchUtils.GetChildrenByHierarchical<RaymarchRenderer>(transform);
        }
#endif
        
        public void AddRenderer(RaymarchRenderer raymarchRenderer)
        {
            if (raymarchRenderers.Contains(raymarchRenderer)) return;
            
            raymarchRenderers.Add(raymarchRenderer);
            OnAddRenderer?.Invoke(raymarchRenderer);
        }

        public void RemoveRenderer(RaymarchRenderer raymarchRenderer)
        {
            if (!raymarchRenderers.Contains(raymarchRenderer)) return;

            raymarchRenderers.Remove(raymarchRenderer);
            OnRemoveRenderer?.Invoke(raymarchRenderer);
        }
    }
}
