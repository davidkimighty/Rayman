#ifndef RAYMAN_RAYMARCH
#define RAYMAN_RAYMARCH

#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/Ray.hlsl"

#define EPSILON (0.001)

// Must be implemented by the including shader.
inline float Map(inout Ray ray);

inline bool Raymarch(inout Ray ray)
{
    for (int i = 0; i < ray.maxSteps; i++)
    {
        ray.lastHitDistance = Map(ray);
        ray.travelDistance += ray.lastHitDistance;
        ray.hitPoint += ray.dir * ray.lastHitDistance;
        if (ray.lastHitDistance < EPSILON || ray.travelDistance > ray.maxDistance) break;
    }
    return ray.lastHitDistance < EPSILON;
}

// Must be implemented by the including shader.
inline float NormalMap(const float3 rayPos);

inline float3 GetNormal(const float3 pos)
{
    float3 x = float3(EPSILON, 0, 0);
    float3 y = float3(0, EPSILON, 0);
    float3 z = float3(0, 0, EPSILON);

    float distX = NormalMap(pos + x) - NormalMap(pos - x);
    float distY = NormalMap(pos + y) - NormalMap(pos - y);
    float distZ = NormalMap(pos + z) - NormalMap(pos - z);
    return normalize(float3(distX, distY, distZ));
}

#endif