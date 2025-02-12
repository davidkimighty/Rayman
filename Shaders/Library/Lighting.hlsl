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

float GGXSpecular(float3 normal, float3 viewDirection, float3 lightDirection, float f0, float roughness)
{
    float alpha = roughness * roughness;
    float3 h = normalize(viewDirection + lightDirection);
    float ndoth = saturate(dot(normal, h));
    if (ndoth < 0.0001) return 0.0;

    float ndotl = saturate(dot(normal, lightDirection));
    float ndotv = saturate(dot(normal, viewDirection));

    float nom   = alpha * alpha;
    float denom = (ndoth * ndoth * (alpha * alpha - 1.0) + 1.0);
    float d = nom / (PI * denom * denom);

    float fresnel = f0 + (1.0 - f0) * pow(1.0 - ndotl, 5);

    float vis_num   = 2.0 * ndotl * ndotv;
    float vis_denom = ndotl * sqrt(1.0 - ndotv * ndotv) + ndotv * sqrt(1.0 - ndotl * ndotl);
    float vis = vis_num / max(0.0001, vis_denom);

    return fresnel * d * vis;
}

inline float3 GetFresnel(const float3 viewDir, const float3 normalWS, const float power)
{
    return pow(1.0 - saturate(dot(viewDir, normalWS)), power);
}

// Schlick approximation
inline float3 GetFresnelSchlick(const float3 viewDir, const float3 normalWS, const float3 f0)
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

inline float3 RGB2HSV(float3 rgb)
{
    float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    float4 P = lerp(float4(rgb.bg, K.wz), float4(rgb.gb, K.xy), step(rgb.b, rgb.g));
    float4 Q = lerp(float4(P.xyw, rgb.r), float4(rgb.r, P.yzx), step(P.x, rgb.r));
    float D = Q.x - min(Q.w, Q.y);
    float  E = 1e-10;
    float V = (D == 0) ? Q.x : (Q.x + E);
    return float3(abs(Q.z + (Q.w - Q.y)/(6.0 * D + E)), D / (Q.x + E), V);
}

#endif