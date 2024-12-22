using System.Collections.Generic;
using UnityEngine;

namespace Rayman
{
    [ExecuteInEditMode]
    public class RaymarchGroup : MonoBehaviour
    {
        [SerializeField] private List<RaymarchShape> shapes = new();
        
        public List<RaymarchShape> Shapes => shapes;
        
#if UNITY_EDITOR
        [ContextMenu("Find All Shapes")]
        private void FindAllShapes()
        {
            shapes = Utilities.GetChildrenByHierarchical<RaymarchShape>(transform);
        }
#endif
    }
}
