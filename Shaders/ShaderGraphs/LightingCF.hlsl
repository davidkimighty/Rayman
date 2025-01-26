#ifndef RAYMAN_LIGHTING_CF
#define RAYMAN_LIGHTING_CF

#ifndef SHADERGRAPH_PREVIEW
#include "Packages/com.davidkimighty.rayman/Shaders/Library/Lighting.hlsl"
#endif

inline void AdditionalLightsSSSs_float(float3 pos, float3 viewDir, float3 normal,
    float distortion, float power, float scale, float ambient, float thickness, out float3 result)
{
    result = float3(0, 0, 0);
#ifndef SHADERGRAPH_PREVIEW
    const int count = GetAdditionalLightsCount();
    for (int i = 0; i < count; ++i)
    {
        const Light light = GetAdditionalLight(i, pos);
        result += light.color * SSSApproximation(light, viewDir, normal, distortion, power, scale, ambient, thickness);
    }
#endif
}

#endif