using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Rayman
{
    public class ScreenDebugger : DebugElement
    {
        public override string GetDebugMessage()
        {
            var upscaling = UniversalRenderPipeline.asset.upscalingFilter.ToString();
            float renderScale = UniversalRenderPipeline.asset.renderScale;
            return $"Resolution {Screen.width,4} x {Screen.height,4} [ {upscaling} Scale {renderScale,1} ]";
        }
    }
}
