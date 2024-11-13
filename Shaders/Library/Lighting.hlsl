#ifndef RAYMAN_LIGHTING
#define RAYMAN_LIGHTING

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

inline float GetDiffuse(float3 lightDir, float3 normal)
{
    return saturate(dot(normal, lightDir));
}

inline float GetSpecular(float3 rayDir, float3 lightDir, float3 normal, float power)
{
    const float3 dir = normalize(lightDir - rayDir);
    return pow(clamp(dot(normal, dir), 0., 1.), power);
}

// Schlick approximation
inline float3 GetFresnelSchlick(float3 f0, float3 viewDir, float3 normalWS)
{
    float cosTheta = saturate(dot(normalWS, viewDir));
    return f0 + (1.0 - f0) * pow(1.0 - cosTheta, 5.0);
}

inline float3 GammaCorrection(float3 color, float dt)
{
    color *= exp(-0.0005 * dt * dt * dt);
    return pow(color, float3(0.4545f, 0.4545f, 0.4545f));
}

#endif