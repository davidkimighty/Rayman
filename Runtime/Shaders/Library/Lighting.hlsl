#ifndef RAYMAN_LIGHTING
#define RAYMAN_LIGHTING

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

float GetDiffuse(float3 lightDir, float3 normal)
{
    return saturate(dot(normal, lightDir));
}

float GetSpecular(float3 rayDir, float3 lightDir, float3 normal, float power)
{
    const float3 dir = normalize(lightDir - rayDir);
    return pow(clamp(dot(normal, dir), 0., 1.), power);
}

float3 GammaCorrection(float3 color, float dt)
{
    color *= exp(-0.0005 * dt * dt * dt);
    return pow(color, float3(0.4545f, 0.4545f, 0.4545f));
}

#endif