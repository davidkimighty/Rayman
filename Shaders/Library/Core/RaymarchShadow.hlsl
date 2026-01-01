#ifndef RAYMAN_RAYMARCH_SHADOW
#define RAYMAN_RAYMARCH_SHADOW

#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/Ray.hlsl"

// Must be implemented by the including shader.
inline float ShadowMap(const float3 positionWS);

inline float GetHardShadow(inout Ray ray, const int maxSteps, const int maxDistance,
    const float epsilonMin, const float epsilonMax)
{
    ray.travelDist = 0.6;
    for (int i = 0; i < maxSteps; i++)
    {
        float dist = ShadowMap(ray.origin + ray.dir * ray.travelDist);
        float e = lerp(epsilonMin, epsilonMax, saturate(ray.travelDist / maxDistance));
        if(dist < e)
            return 0;
        if (ray.travelDist > maxDistance) break;
        ray.travelDist += dist;
    }
    return 1;
}

inline float GetSoftShadow(in Ray ray, const int maxSteps, const int maxDistance,
    const float epsilonMin, const float epsilonMax, const float w)
{
    float res = 1;
    ray.travelDist = epsilonMin;
    for (int i = 0; i < maxSteps; i++)
    {
        const float dist = ShadowMap(ray.origin + ray.dir * ray.travelDist);
        res = min(res, dist / (w * ray.travelDist));
        ray.travelDist += clamp(dist, 0.005, 0.5);
        if (res < -1 || ray.travelDist > maxDistance) break;
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