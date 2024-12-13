#ifndef RAYMAN_AABB
#define RAYMAN_AABB

#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/Ray.hlsl"

struct AABB
{
    float3 min;
    float3 max;
};

inline bool Intersect(const AABB a, const AABB b)
{
    return all(a.min <= b.max && a.max >= b.min);
}

inline float3 Extents(const AABB aabb)
{
    return aabb.max - aabb.min;
}

inline float3 HalfExtents(const AABB aabb)
{
    return 0.5 * (aabb.max - aabb.min);
}

inline float Center(const AABB aabb)
{
    return 0.5 * (aabb.min + aabb.max);
}

inline bool RayIntersect(const Ray ray, const AABB aabb, out float tMin, out float tMax)
{
    float3 invDir = 1.0 / ray.dir * ray.maxDistance;
    float3 t1 = (aabb.min - ray.origin) * invDir;
    float3 t2 = (aabb.max - ray.origin) * invDir;
    float3 minComps = min(t1, t2);
    float3 maxComps = max(t1, t2);
    
    tMin = max(max(minComps.x, minComps.y), minComps.z);
    tMax = min(min(maxComps.x, maxComps.y), maxComps.z);
    return tMax >= max(tMin, 0.0);
}

#endif