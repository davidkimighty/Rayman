#ifndef RAYMAN_LIGHTING
#define RAYMAN_LIGHTING

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

inline float GetDiffuse(const float3 lightDir, const float3 normal)
{
    return saturate(dot(normal, lightDir));
}

// Blinn-Phong Specular Reflection
inline float GetSpecular(const float3 rayDir, const float3 lightDir, const float3 normal, const float power)
{
    const float3 dir = normalize(lightDir - rayDir);
    return pow(clamp(dot(normal, dir), 0., 1.), power);
}

float G1V(float dnv, float k)
{
    return 1.0 / (dnv * (1.0 - k) + k);
}

float GGXSpecular(float3 n, float3 v, float3 l, float f0, float roughness)
{
    //roughness = 1.0 - roughness;
    float alpha = roughness * roughness; 
    float alphaSq = alpha * alpha;
    float3 h = normalize(v + l);

    float dnl = saturate(dot(n, l));
    float dnv = saturate(dot(n, v));
    float dnh = saturate(dot(n, h));
    float dlh = saturate(dot(l, h));

    float denom = dnh * dnh * (alphaSq - 1.0) + 1.0;
    float d = alphaSq / (PI * denom * denom);
    
    float dlhPow5 = pow(1.0 - dlh, 5.0);
    float f = f0 + (1.0 - f0) * dlhPow5;
    
    float k = alpha / 2.0;
    float vis = G1V(dnl, k) * G1V(dnv, k);
    return dnl * d * vis * dot(f, 1.0);
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

// GDC 2011 – Approximating Translucency for a Fast, Cheap and Convincing Subsurface Scattering Look
inline float3 SSSApproximation(Light light, const float3 viewDir, const float3 normal, const float distortion,
    const float power, const float scale, const float ambient, const float thickness)
{
    float3 vLTLight = light.direction + normal * distortion;
    float fLTDot = pow(saturate(dot(viewDir, -vLTLight)), power) * scale;
    float3 fLT = light.distanceAttenuation * (fLTDot + ambient) * thickness;
    return fLT;
}

inline float3 GammaCorrection(float3 color, const float dt)
{
    color *= exp(-0.0005 * dt * dt * dt);
    return pow(color, float3(0.4545f, 0.4545f, 0.4545f));
}

#endif