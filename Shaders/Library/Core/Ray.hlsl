#ifndef RAYMAN_RAY
#define RAYMAN_RAY

#define RAY_MAX_HITS 8

struct Ray
{
    float3 origin;
    float3 dir;
    float3 invDir;
    int maxSteps;
    float maxDistance;
    float3 hitPoint;
    float distanceTravelled;
    float hitDistance;
};

inline Ray CreateRay(const float3 origin, const float3 dir, const int maxSteps, const float maxDistance)
{
    Ray ray = (Ray)0;
    ray.origin = origin;
    ray.dir = dir;
    ray.invDir = 1 / ray.dir;
    ray.maxSteps = maxSteps;
    ray.maxDistance = maxDistance;
    ray.hitPoint = ray.origin;
    ray.distanceTravelled = 0;
    ray.hitDistance = 0;
    return ray;
}

#endif