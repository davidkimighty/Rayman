using UnityEngine;
using UnityEngine.Rendering;

namespace Rayman
{
    [CreateAssetMenu(menuName = "Rayman/Data/Render State")]
    public class RenderStateData : ScriptableObject
    {
        public BlendMode SrcBlend = BlendMode.One;
        public BlendMode DstBlend = BlendMode.Zero;
        public CullMode Cull = CullMode.Back;
        public bool ZWrite = true;
    }
}
