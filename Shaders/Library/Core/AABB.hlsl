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

inline bool RayIntersect(const Ray ray, const AABB aabb)
{
    float3 invDir = 1.0 / ray.dir * ray.maxDistance;
    float3 tMin = (aabb.min - ray.origin) * invDir;
    float3 tMax = (aabb.max - ray.origin) * invDir;
    float3 t1 = min(tMin, tMax);
    float3 t2 = max(tMin, tMax);
    float dstFar = min(min(t2.x, t2.y), t2.z);
    float dstNear = max(max(t1.x, t1.y), t1.z);
    return dstFar >= dstNear && dstFar > 0;
}

inline bool RayIntersect(const Ray ray, const AABB aabb, out float dstNear, out float dstFar)
{
    float3 invDir = 1.0 / ray.dir * ray.maxDistance;
    float3 tMin = (aabb.min - ray.origin) * invDir;
    float3 tMax = (aabb.max - ray.origin) * invDir;
    float3 t1 = min(tMin, tMax);
    float3 t2 = max(tMin, tMax);
    dstFar = min(min(t2.x, t2.y), t2.z);
    dstNear = max(max(t1.x, t1.y), t1.z);
    return dstFar >= dstNear && dstFar > 0;
}

#endif