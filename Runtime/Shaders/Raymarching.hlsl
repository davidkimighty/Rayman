#ifndef RAYMAN_RAYMARCHING
#define RAYMAN_RAYMARCHING

#include "Packages/com.davidkimighty.rayman/Runtime/Shaders/Camera.hlsl"
#include "Packages/com.davidkimighty.rayman/Runtime/Shaders/TransformSpace.hlsl"

struct Ray
{
    float3 origin;
    float3 dir;
    int maxSteps;
    float maxDist;
    float currentDist;
    float distTravelled;
    float3 travelledPoint;
};

inline Ray InitRay(const float3 origin, const int maxSteps, const float maxDist)
{
    Ray ray = (Ray)0;
    ray.origin = origin;
    ray.dir = normalize(origin - GetCameraPosition());
    ray.maxSteps = maxSteps;
    ray.maxDist = maxDist;
    return ray;
}

inline float GetDepth(const Ray ray, const float3 wsPos)
{
    float lengthToSurface = length(wsPos - GetCameraPosition());
    return ray.distTravelled - lengthToSurface < 0.001 ? GetDepth(wsPos) : GetDepth(ray.travelledPoint);
}

#endif