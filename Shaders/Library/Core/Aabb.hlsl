#ifndef RAYMAN_AABB
#define RAYMAN_AABB

struct Aabb
{
    float3 min;
    float3 max;
};

bool Intersect(Aabb a, Aabb b)
{
    return all(a.min <= b.max && a.max >= b.min);
}

float3 Extents(Aabb aabb)
{
    return aabb.max - aabb.min;
}

float3 HalfExtents(Aabb aabb)
{
    return 0.5 * (aabb.max - aabb.min);
}

float Center(Aabb aabb)
{
    return 0.5 * (aabb.min + aabb.max);
}

bool RayIntersect(float3 origin, float3 invDir, float3 boundsMin, float3 boundsMax, out float dstNear, out float dstFar)
{
    float3 tMin = (boundsMin - origin) * invDir;
    float3 tMax = (boundsMax - origin) * invDir;
    float3 t1 = min(tMin, tMax);
    float3 t2 = max(tMin, tMax);
    dstNear = max(max(t1.x, t1.y), t1.z);
    dstFar = min(min(t2.x, t2.y), t2.z);
    return dstFar >= dstNear && dstFar > 0;
}

bool RayIntersect(float3 origin, float3 invDir, float3 boundsMin, float3 boundsMax, out float dstNear)
{
    float dstFar;
    return RayIntersect(origin, invDir, boundsMin, boundsMax, dstNear, dstFar);
}

bool RayIntersect(float3 origin, float3 invDir, float3 boundsMin, float3 boundsMax)
{
    float dstNear;
    float dstFar;
    return RayIntersect(origin, invDir, boundsMin, boundsMax, dstNear, dstFar);
}

#endif