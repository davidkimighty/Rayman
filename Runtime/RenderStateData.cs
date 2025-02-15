using UnityEngine;
using UnityEngine.Rendering;

namespace Rayman
{
    public class RenderStateData : MonoBehaviour
    {
        public BlendMode SrcBlend = BlendMode.One;
        public BlendMode DstBlend = BlendMode.Zero;
        public CullMode Cull = CullMode.Back;
        public bool ZWrite = true;
    }
}
