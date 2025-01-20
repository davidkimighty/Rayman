using System.Collections.Generic;
using UnityEngine;

namespace Rayman
{
    public class ComputeRaymarchRenderer : MonoBehaviour
    {
        [SerializeField] protected List<RaymarchEntity> shapes = new();

        public List<RaymarchEntity> Shapes => shapes;
        
#if UNITY_EDITOR
        [ContextMenu("Find All Shapes")]
        private void FindAllShapes()
        {
            shapes = RaymarchUtils.GetChildrenByHierarchical<RaymarchEntity>(transform);
        }
#endif
    }
}