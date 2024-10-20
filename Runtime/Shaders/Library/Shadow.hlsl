#ifndef RAYMAN_SHADOW
#define RAYMAN_SHADOW

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

const float shadowBias = 0.0135;

float GetShadowAttenuationWithBias(float3 wsPos, float wsNormal, float3 lightDir)
{
    float normalBias = shadowBias * max(0.0, dot(lightDir, wsNormal));
    half4 shadowCoord = TransformWorldToShadowCoord(wsPos);
    shadowCoord.z += normalBias;
    Light mainLight = GetMainLight(shadowCoord);
    return mainLight.shadowAttenuation;
}

#endif