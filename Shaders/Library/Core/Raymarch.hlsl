#ifndef RAYMAN_RAYMARCH
#define RAYMAN_RAYMARCH

#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/Ray.hlsl"

inline float Map(const float3 positionWS);

inline bool Raymarch(inout Ray ray, const int maxSteps, const int maxDistance, const float epsilonMin, const float epsilonMax)
{
    float travelled = ray.distanceTravelled;
    [loop]
    for (int i = 0; i < maxSteps; i++)
    {
        float3 travelledPos = ray.origin + ray.dir * travelled;
        float hitDistance = Map(travelledPos);
        float epsilon = lerp(epsilonMin, epsilonMax, saturate(travelled / maxDistance));
        if (hitDistance < epsilon || travelled > maxDistance)
        {
            ray.distanceTravelled = travelled;
            ray.hitPoint = travelledPos;
            return hitDistance < epsilon;
        }
        travelled += hitDistance;
    }
    ray.distanceTravelled = travelled;
    ray.hitPoint = ray.origin + ray.dir * travelled;
    return false;
}

inline bool RaymarchHitCount(inout Ray ray, const int maxSteps, const int maxDistance,
    const float epsilonMin, const float epsilonMax, out int hitCount)
{
    hitCount = 0;
    float travelled = ray.distanceTravelled;
    [loop]
    for (int i = 0; i < maxSteps; i++)
    {
        float3 travelledPos = ray.origin + ray.dir * travelled;
        float hitDistance = Map(travelledPos);
        float epsilon = lerp(epsilonMin, epsilonMax, saturate(travelled / maxDistance));
        if (hitDistance < epsilon || travelled > maxDistance)
        {
            ray.distanceTravelled = travelled;
            ray.hitPoint = travelledPos;
            return hitDistance < epsilon;
        }
        travelled += hitDistance;
        hitCount++;
    }
    ray.distanceTravelled = travelled;
    ray.hitPoint = ray.origin + ray.dir * travelled;
    return false;
}

#endif