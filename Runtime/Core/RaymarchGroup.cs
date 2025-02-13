using UnityEngine;
using UnityEngine.Rendering;

namespace Rayman
{
    public abstract class RaymarchGroup : MonoBehaviour
    {
        protected static readonly int SrcBlendId = Shader.PropertyToID("_SrcBlend");
        protected static readonly int DstBlendId = Shader.PropertyToID("_DstBlend");
        protected static readonly int CullId = Shader.PropertyToID("_Cull");
        protected static readonly int ZWriteId = Shader.PropertyToID("_ZWrite");
        
        [SerializeField] protected Shader shader;
        // [SerializeField] private BlendMode srcBlend = BlendMode.One;
        // [SerializeField] private BlendMode dstBlend = BlendMode.Zero;
        // [SerializeField] private CullMode cull = CullMode.Back;
        // [SerializeField] private bool zWrite = true;
#if UNITY_EDITOR
        [SerializeField] protected bool drawGizmos;
#endif
        
        public abstract bool IsInitialized();
        public abstract Material InitializeGroup();
        public abstract void ReleaseGroup();
    }
}
