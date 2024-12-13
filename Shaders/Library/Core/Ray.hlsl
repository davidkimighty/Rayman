#ifndef RAYMAN_RAY
#define RAYMAN_RAY

#define RAY_MAX_HITS 16

struct Ray
{
    float3 origin;
    float3 dir;
    int maxSteps;
    float maxDistance;
    float3 hitPoint;
    float travelDistance;
    float lastHitDistance;
};

inline Ray CreateRay(const float3 origin, const float3 dir, const int maxSteps, const float maxDistance)
{
    Ray ray = (Ray)0;
    ray.origin = origin;
    ray.dir = dir;
    ray.maxSteps = maxSteps;
    ray.maxDistance = maxDistance;
    ray.hitPoint = ray.origin;
    ray.travelDistance = 0;
    ray.lastHitDistance = 0;
    return ray;
}

#endif