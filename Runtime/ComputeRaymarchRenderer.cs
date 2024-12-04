using System.Collections.Generic;
using UnityEngine;

namespace Rayman
{
    [ExecuteInEditMode]
    public class ComputeRaymarchRenderer : MonoBehaviour
    {
        [SerializeField] private RaymarchFeature raymarchFeature;
        [SerializeField] private bool selfRegister;
        [SerializeField] private List<RaymarchShape> shapes = new();

        [SerializeField] private Renderer raymarchRenderer;
        
        public List<RaymarchShape> Shapes => shapes;

        private void OnEnable()
        {
            if (selfRegister)
                RegisterRenderer();
        }

        private void OnDisable()
        {
            if (selfRegister)
                DeregisterRenderer();
        }

        [ContextMenu("Register Renderer")]
        public void RegisterRenderer()
        {
            raymarchFeature?.AddRenderer(this);
        }

        [ContextMenu("Deregister Renderer")]
        public void DeregisterRenderer()
        {
            raymarchFeature?.RemoveRenderer(this);
        }
        
#if UNITY_EDITOR
        [ContextMenu("Find All Shapes")]
        private void FindAllShapes()
        {
            shapes = Utilities.GetObjectsByTypes<RaymarchShape>(transform);
        }
#endif
    }
}

