#ifndef RAYMAN_RAYMARCH
#define RAYMAN_RAYMARCH

#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/Math.hlsl"
#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/Ray.hlsl"

// Must be implemented by the including shader.
float Map(const float3 positionWS);

inline bool Raymarch(inout Ray ray)
{
    for (int i = 0; i < ray.maxSteps; i++)
    {
        ray.hitDistance = Map(ray.hitPoint);
        ray.distanceTravelled += ray.hitDistance;
        ray.hitPoint += ray.dir * ray.hitDistance;
        if (ray.hitDistance < EPSILON || ray.distanceTravelled > ray.maxDistance) break;
    }
    return ray.hitDistance < EPSILON;
}

// Must be implemented by the including shader.
float NormalMap(const float3 positionWS);

inline float3 GetNormal(const float3 positionWS)
{
    float3 x = float3(EPSILON, 0, 0);
    float3 y = float3(0, EPSILON, 0);
    float3 z = float3(0, 0, EPSILON);

    float distX = NormalMap(positionWS + x) - NormalMap(positionWS - x);
    float distY = NormalMap(positionWS + y) - NormalMap(positionWS - y);
    float distZ = NormalMap(positionWS + z) - NormalMap(positionWS - z);
    return normalize(float3(distX, distY, distZ));
}

inline float3 GetHitMap(const int hit, const int maxSteps, const float3 col1, const float3 col2)
{
    float n = clamp(float(hit) / float(maxSteps), 0.0, 1.0);
    return float3(lerp(col1, col2, smoothstep(0.0, 1.0, n)));
}

inline bool RaymarchHitCount(inout Ray ray, out int count)
{
    count = 0;
    for (int i = 0; i < ray.maxSteps; i++)
    {
        ray.hitDistance = Map(ray.hitPoint);
        ray.distanceTravelled += ray.hitDistance;
        ray.hitPoint += ray.dir * ray.hitDistance;
        if (ray.hitDistance < EPSILON || ray.distanceTravelled > ray.maxDistance) break;
        count++;
    }
    return ray.hitDistance < EPSILON;
}

#endif