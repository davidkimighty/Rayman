using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rayman
{
    public class ComputeRaymarchRenderer : MonoBehaviour
    {
        [SerializeField] private RaymarchFeature raymarchFeature;
        [SerializeField] private Renderer raymarchRenderer;
        [SerializeField] private Material matRef;
        [SerializeField] private RaymarchSetting setting;
        [SerializeField] private List<RaymarchShape> shapes = new();

        public List<RaymarchShape> Shapes => shapes;

        private void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += RegisterRenderer;
        }

        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= RegisterRenderer;
        }

        private void RegisterRenderer(ScriptableRenderContext context, Camera camera)
        {
            raymarchFeature.ComputePass.AddRenderer(this);
        }
    }
}

