#ifndef RAYMAN_RAY
#define RAYMAN_RAY

#define RAY_MAX_DISTANCE 100.0
#define RAY_MAX_HITS 16

struct Ray
{
    float3 origin;
    float3 dir;
    float3 hitPoint;
    float travelDist;
    float minDist;
    float minDistTravelDist;
};

inline Ray CreateRay(const float3 origin, const float3 dir)
{
    Ray ray = (Ray)0;
    ray.origin = origin;
    ray.dir = dir;
    ray.hitPoint = ray.origin;
    ray.travelDist = 0;
    ray.minDist = RAY_MAX_DISTANCE;
    return ray;
}

#endif