#ifndef RAYMAN_RAYMARCH
#define RAYMAN_RAYMARCH

#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/Ray.hlsl"

inline float Map(const float3 positionWS);

inline bool Raymarch(inout Ray ray, const int maxSteps, const int maxDistance, const float epsilonMin, const float epsilonMax)
{
    float travelled = ray.travelDist;
    [loop]
    for (int i = 0; i < maxSteps; i++)
    {
        float3 pos = ray.origin + ray.dir * travelled;
        float dist = Map(pos);
        if (dist < ray.minDist)
        {
            ray.minDist = dist;
            ray.minDistTravelDist = travelled;
        }
        float epsilon = lerp(epsilonMin, epsilonMax, saturate(travelled / maxDistance));
        if (dist < epsilon || travelled > maxDistance)
        {
            ray.travelDist = travelled;
            ray.hitPoint = pos;
            return dist < epsilon;
        }
        travelled += dist;
    }
    ray.travelDist = travelled;
    ray.hitPoint = ray.origin + ray.dir * travelled;
    return false;
}

inline bool RaymarchHitCount(inout Ray ray, const int maxSteps, const int maxDistance,
    const float epsilonMin, const float epsilonMax, out int hitCount)
{
    hitCount = 0;
    float travelled = ray.travelDist;
    [loop]
    for (int i = 0; i < maxSteps; i++)
    {
        float3 pos = ray.origin + ray.dir * travelled;
        float dist = Map(pos);
        if (dist < ray.minDist)
        {
            ray.minDist = dist;
            ray.minDistTravelDist = travelled;
        }
        float epsilon = lerp(epsilonMin, epsilonMax, saturate(travelled / maxDistance));
        if (dist < epsilon || travelled > maxDistance)
        {
            ray.travelDist = travelled;
            ray.hitPoint = pos;
            return dist < epsilon;
        }
        travelled += dist;
        hitCount++;
    }
    ray.travelDist = travelled;
    ray.hitPoint = ray.origin + ray.dir * travelled;
    return false;
}

#endif