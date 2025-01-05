#ifndef RAYMAN_LIGHTING
#define RAYMAN_LIGHTING

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/Ray.hlsl"

inline float GetDiffuse(const float3 lightDir, const float3 normal)
{
    return saturate(dot(normal, lightDir));
}

inline float GetSpecular(const float3 rayDir, const float3 lightDir, const float3 normal, const float power)
{
    const float3 dir = normalize(lightDir - rayDir);
    return pow(clamp(dot(normal, dir), 0., 1.), power);
}

inline float3 GetFresnel(const float3 viewDir, const float3 normalWS, const float power)
{
    return pow(1.0 - saturate(dot(viewDir, normalWS)), power);
}

// Schlick approximation
inline float3 GetFresnelSchlick(const float3 viewDir, const float3 normalWS, const float f0)
{
    float cosTheta = saturate(dot(normalWS, viewDir));
    return f0 + (1.0 - f0) * pow(1.0 - cosTheta, 5.0);
}

inline float3 GammaCorrection(float3 color, const float dt)
{
    color *= exp(-0.0005 * dt * dt * dt);
    return pow(color, float3(0.4545f, 0.4545f, 0.4545f));
}

inline float3 MainLightShade(const float3 pos, const float3 dir, float shadowBias, const float3 normal,
    const float fresnel, const float specularPow)
{
    float4 shadowCoord = TransformWorldToShadowCoord(pos);
    const Light mainLight = GetMainLight(shadowCoord);

    const float normalBias = shadowBias * max(0.0, dot(mainLight.direction, normal));
    shadowCoord.z += normalBias;
    const Light mainLightWithBias = GetMainLight(shadowCoord);
    float3 shade = mainLight.color *  mainLightWithBias.shadowAttenuation;
	
    const float mainDiffuse = GetDiffuse(mainLight.direction, normal);
    float mainSpecular = GetSpecular(dir, mainLight.direction, normal, specularPow * 100);
    mainSpecular *= mainDiffuse * fresnel;
    shade *= mainDiffuse + mainSpecular;
    return shade;
}

inline void AdditionalLightsShade(const float3 pos, const float3 dir, const float3 normal,
    const float fresnel, const float specularPow, inout float3 shade)
{
    const int count = GetAdditionalLightsCount();
    for (int i = 0; i < count; ++i)
    {
        const Light light = GetAdditionalLight(i, pos);
        const float diffuse = GetDiffuse(light.direction, normal) * light.distanceAttenuation;
        float specular = GetSpecular(dir, light.direction, normal, specularPow * 100);
        specular *= diffuse * fresnel;
        shade += light.color * (diffuse + specular);
    }
}

inline float3 RimLightShade(const float3 normal, const float3 viewDir, const float rimPower, const float3 rimColor)
{
    float rimIntensity = pow(1.0 - saturate(dot(normal, viewDir)), 1.0 / rimPower);
    return rimColor * rimIntensity;
}

#endif