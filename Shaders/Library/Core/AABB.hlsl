#ifndef RAYMAN_AABB
#define RAYMAN_AABB

struct Aabb
{
    float3 min;
    float3 max;
};

inline bool Intersect(const Aabb a, const Aabb b)
{
    return all(a.min <= b.max && a.max >= b.min);
}

inline float3 Extents(const Aabb aabb)
{
    return aabb.max - aabb.min;
}

inline float3 HalfExtents(const Aabb aabb)
{
    return 0.5 * (aabb.max - aabb.min);
}

inline float Center(const Aabb aabb)
{
    return 0.5 * (aabb.min + aabb.max);
}

inline bool DidHit(const float dstNear, const float dstFar)
{
    return dstFar >= dstNear && dstFar > 0;
}

inline bool RayIntersect(const float3 origin, const float3 invDir, const Aabb aabb)
{
    float3 tMin = (aabb.min - origin) * invDir;
    float3 tMax = (aabb.max - origin) * invDir;
    float3 t1 = min(tMin, tMax);
    float3 t2 = max(tMin, tMax);
    float dstNear = max(max(t1.x, t1.y), t1.z);
    float dstFar = min(min(t2.x, t2.y), t2.z);
    return DidHit(dstNear, dstFar);
}

inline float RayIntersectNearDst(const float3 origin, const float3 invDir, const Aabb aabb)
{
    float3 tMin = (aabb.min - origin) * invDir;
    float3 tMax = (aabb.max - origin) * invDir;
    float3 t1 = min(tMin, tMax);
    float3 t2 = max(tMin, tMax);
    float dstNear = max(max(t1.x, t1.y), t1.z);
    float dstFar = min(min(t2.x, t2.y), t2.z);
    bool didHit = dstFar >= dstNear && dstFar > 0;
    return didHit ? dstNear : 100.0;
}

#endif