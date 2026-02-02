#ifndef RAYMAN_RAY
#define RAYMAN_RAY

#ifndef RAY_MAX_DISTANCE
#define RAY_MAX_DISTANCE 100.0
#endif

#ifndef RAY_MAX_HITS
#define RAY_MAX_HITS 16
#endif

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