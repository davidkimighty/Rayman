using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rayman
{
    [ExecuteInEditMode]
    public class ShapeGroupObject : MonoBehaviour
    {
        [Serializable]
        public class MaterialProvider
        {
            [SerializeField] private MaterialDataProvider materialData;
            [SerializeField] private ShapeProvider[] shapeProviders;
        }

        [SerializeField] private Renderer mainRenderer;
        // ray setting so?
        [SerializeField] private Shader shader;
        [SerializeField] private MaterialProvider[] materialProviders;

        private void LateUpdate()
        {
            
        }

        private void OnDestroy()
        {
            
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            
        }
#endif
    }
}
