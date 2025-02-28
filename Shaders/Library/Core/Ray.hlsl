#ifndef RAYMAN_RAY
#define RAYMAN_RAY

#define RAY_MAX_HITS 16

struct Ray
{
    float3 origin;
    float3 dir;
    float epsilon;
    float3 hitPoint;
    float distanceTravelled;
    float hitDistance;
    bool hit;
};

inline Ray CreateRay(const float3 origin, const float3 dir, const float epsilon)
{
    Ray ray = (Ray)0;
    ray.origin = origin;
    ray.dir = dir;
    ray.epsilon = epsilon;
    ray.hitPoint = ray.origin;
    ray.distanceTravelled = 0;
    ray.hitDistance = 0;
    return ray;
}

#endif