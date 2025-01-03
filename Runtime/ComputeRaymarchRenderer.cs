using System.Collections.Generic;
using UnityEngine;

namespace Rayman
{
    public class ComputeRaymarchRenderer : MonoBehaviour
    {
        [SerializeField] protected List<RaymarchShape> shapes = new();

        public List<RaymarchShape> Shapes => shapes;
        
#if UNITY_EDITOR
        [ContextMenu("Find All Shapes")]
        private void FindAllShapes()
        {
            shapes = RaymarchUtils.GetChildrenByHierarchical<RaymarchShape>(transform);
        }
#endif
    }
}