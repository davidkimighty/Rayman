using System.Collections.Generic;
using UnityEngine;

namespace Rayman
{
    [ExecuteInEditMode]
    public class ComputeRaymarchRenderer : MonoBehaviour
    {
        [SerializeField] private RaymarchFeature raymarchFeature;
        [SerializeField] private List<RaymarchShape> shapes = new();

        [SerializeField] private Renderer raymarchRenderer;
        
        public List<RaymarchShape> Shapes => shapes;

#if UNITY_EDITOR
        [ContextMenu("Find All Shapes")]
        private void FindAllShapes()
        {
            shapes = Utilities.GetObjectsByTypes<RaymarchShape>(transform);
        }
#endif
    }
}

