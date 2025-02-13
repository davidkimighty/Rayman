using System.Collections.Generic;
using UnityEngine;

namespace Rayman
{
    public abstract class RaymarchGroup : MonoBehaviour
    {
        [SerializeField] protected Shader shader;
        [SerializeField] protected List<RaymarchEntity> entities = new();
#if UNITY_EDITOR
        [SerializeField] protected bool drawGizmos;
#endif
        protected RaymarchEntity[] activeEntities;
        
        public abstract Material InitializeGroup();
        public abstract void ReleaseGroup();
        
        public virtual bool IsInitialized() => activeEntities != null;
        
#if UNITY_EDITOR
        [ContextMenu("Find all entities")]
        public void FindAllEntities()
        {
            entities = RaymarchUtils.GetChildrenByHierarchical<RaymarchEntity>(transform);
        }
#endif
    }
}
