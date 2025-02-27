#ifndef RAYMAN_RAY
#define RAYMAN_RAY

#define RAY_MAX_HITS 16

struct Ray
{
    float3 origin;
    float3 dir;
    float3 invDir;
    float3 epsilon;
    int maxSteps;
    float maxDistance;
    float3 hitPoint;
    float distanceTravelled;
    float hitDistance;
};

inline Ray CreateRay(const float3 origin, const float3 dir, const float2 epsilon,
    const int maxSteps, const float maxDistance)
{
    Ray ray = (Ray)0;
    ray.origin = origin;
    ray.dir = dir;
    ray.invDir = rcp(ray.dir);
    ray.epsilon = float3(epsilon, epsilon.x);
    ray.maxSteps = maxSteps;
    ray.maxDistance = maxDistance;
    ray.hitPoint = ray.origin;
    ray.distanceTravelled = 0;
    ray.hitDistance = 0;
    return ray;
}

#endif