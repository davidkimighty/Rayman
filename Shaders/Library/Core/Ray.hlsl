#ifndef RAYMAN_RAY
#define RAYMAN_RAY

struct Ray
{
    float3 origin;
    float3 dir;
    int maxSteps;
    float maxDistance;
    float travelDistance;
    float3 hitPoint;
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
    return ray;
}

#endif