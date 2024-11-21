#ifndef RAYMAN_LIGHTING
#define RAYMAN_LIGHTING

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

float _ShadowBiasVal;
float _F0;
float _SpecularPow;
half4 _RimColor;
float _RimPow;

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
inline float3 GetFresnelSchlick(float3 viewDir, float3 normalWS)
{
    float cosTheta = saturate(dot(normalWS, viewDir));
    return _F0 + (1.0 - _F0) * pow(1.0 - cosTheta, 5.0);
}

inline float3 GammaCorrection(float3 color, float dt)
{
    color *= exp(-0.0005 * dt * dt * dt);
    return pow(color, float3(0.4545f, 0.4545f, 0.4545f));
}

inline half3 MainLightShade(float3 pos, float3 dir, float3 normal, float fresnel)
{
    half4 shadowCoord = TransformWorldToShadowCoord(pos);
    const Light mainLight = GetMainLight(shadowCoord);

    const float normalBias = _ShadowBiasVal * max(0.0, dot(mainLight.direction, normal));
    shadowCoord.z += normalBias;
    const Light mainLightWithBias = GetMainLight(shadowCoord);
    half3 shade = mainLight.color *  mainLightWithBias.shadowAttenuation;
	
    const float mainDiffuse = GetDiffuse(mainLight.direction, normal);
    float mainSpecular = GetSpecular(dir, mainLight.direction, normal, _SpecularPow * 100);
    mainSpecular *= mainDiffuse * fresnel;
    shade *= mainDiffuse + mainSpecular;
    return shade;
}

inline void AdditionalLightsShade(float3 pos, float3 dir, float3 normal, float fresnel, inout half3 shade)
{
    const int count = GetAdditionalLightsCount();
    for (int i = 0; i < count; ++i)
    {
        const Light light = GetAdditionalLight(i, pos);
        const float diffuse = GetDiffuse(light.direction, normal) * light.distanceAttenuation;
        float specular = GetSpecular(dir, light.direction, normal, _SpecularPow * 100);
        specular *= diffuse * fresnel;
        shade += light.color * (diffuse + specular);
    }
}

inline half3 RimLightShade(float3 normal, float3 viewDir)
{
    float rimIntensity = pow(1.0 - saturate(dot(normal, viewDir)), 1.0 / _RimPow);
    return _RimColor * rimIntensity;
}

#endif