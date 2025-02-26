#ifndef RAYMAN_RAYMARCH_SHADOW
#define RAYMAN_RAYMARCH_SHADOW

#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/Math.hlsl"
#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/Ray.hlsl"

// Must be implemented by the including shader.
float ShadowMap(const float3 positionWS);

inline float GetHardShadow(inout Ray ray)
{
    ray.distanceTravelled = 0.6;
    for (int i = 0; i < ray.maxSteps; i++)
    {
        float dist = ShadowMap(ray.origin + ray.dir * ray.distanceTravelled);
        if(dist < EPSILON)
            return 0;
        if (ray.distanceTravelled > ray.maxDistance) break;
        ray.distanceTravelled += dist;
    }
    return 1;
}

inline float GetSoftShadow(in Ray ray, const float w)
{
    float res = 1;
    ray.distanceTravelled = EPSILON;
    for (int i = 0; i < ray.maxSteps; i++)
    {
        const float dist = ShadowMap(ray.origin + ray.dir * ray.distanceTravelled);
        res = min(res, dist / (w * ray.distanceTravelled));
        ray.distanceTravelled += clamp(dist, 0.005, 0.5);
        if (res < -1 || ray.distanceTravelled > ray.maxDistance) break;
    }
    res = max(res, -1);
    return 0.25 * (1 + res) * (1 + res) * (2 - res);
}

inline float GetAmbientOcclusion(const float3 pos, const float3 normal, const int maxSteps)
{
    float occ = 0;
    float sca = 1;
    for(int i = 0; i < maxSteps; i++)
    {
        float h = 0.01 + 0.12 * float(i) / float(maxSteps);
        float d = ShadowMap(pos + h * normal);
        occ += (h - d) * sca;
        sca *= 0.95;
        if(occ > 0.35) break;
    }
    return clamp(1 - 3 * occ, 0, 1) * (0.5 + 0.5 * normal.y);
}

#endif