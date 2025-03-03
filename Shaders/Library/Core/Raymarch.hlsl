#ifndef RAYMAN_RAYMARCH
#define RAYMAN_RAYMARCH

#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/Ray.hlsl"

// Must be implemented by the including shader.
float Map(inout Ray ray);

bool Raymarch(inout Ray ray, const int maxSteps, const int maxDistance, const float2 epsilon)
{
    for (int i = 0; i < maxSteps; i++)
    {
        ray.hitDistance = Map(ray);
        ray.epsilon = lerp(epsilon.x, epsilon.y, ray.distanceTravelled / maxDistance);
        if (ray.hitDistance < ray.epsilon || ray.distanceTravelled > maxDistance) break;

        ray.distanceTravelled += ray.hitDistance;
        ray.hitPoint += ray.dir * ray.hitDistance;
    }
    return ray.hitDistance < ray.epsilon;
}

bool ConeMarch(inout Ray ray, const int passCount, const int coneSubdiv, const int maxSteps, const int maxDistance,
    const float epsilon, const float tanHalfFov)
{
    for (int p = 0; p < passCount; p++)
    {
        int stepsPerPass = maxSteps / passCount;
        float subdiv = coneSubdiv * pow(2, p);

        for (int i = 0; i < stepsPerPass; i++)
        {
            ray.hitDistance = Map(ray);
            float coneRadius = ray.distanceTravelled * tanHalfFov / subdiv;
            ray.epsilon = lerp(epsilon, coneRadius, ray.distanceTravelled / maxDistance);
            if (ray.hitDistance < coneRadius || ray.hitDistance < ray.epsilon) break;

            float stepSize = max(ray.hitDistance, coneRadius);
            ray.distanceTravelled += stepSize;
            ray.hitPoint += ray.dir * stepSize;

            if (ray.distanceTravelled > maxDistance) return false;
        }
    }
    return true;
}

// Must be implemented by the including shader.
float NormalMap(const float3 positionWS);

float3 GetNormal(const float3 positionWS, const float epsilon)
{
    float3 x = float3(epsilon, 0, 0);
    float3 y = float3(0, epsilon, 0);
    float3 z = float3(0, 0, epsilon);

    float distX = NormalMap(positionWS + x) - NormalMap(positionWS - x);
    float distY = NormalMap(positionWS + y) - NormalMap(positionWS - y);
    float distZ = NormalMap(positionWS + z) - NormalMap(positionWS - z);
    return normalize(float3(distX, distY, distZ));
}

float3 GetHitMap(const int hit, const int maxSteps, const float3 col1, const float3 col2)
{
    float n = clamp(float(hit) / float(maxSteps), 0.0, 1.0);
    return float3(lerp(col1, col2, smoothstep(0.0, 1.0, n)));
}

bool RaymarchHitCount(inout Ray ray, const int maxSteps, const int maxDistance, const float2 epsilon, out int count)
{
    count = 0;
    for (int i = 0; i < maxSteps; i++)
    {
        ray.hitDistance = Map(ray);
        ray.epsilon = lerp(epsilon.x, epsilon.y, ray.distanceTravelled / maxDistance);
        if (ray.hitDistance < ray.epsilon || ray.distanceTravelled > maxDistance) break;
        
        ray.distanceTravelled += ray.hitDistance;
        ray.hitPoint += ray.dir * ray.hitDistance;
        count++;
    }
    return ray.hitDistance < ray.epsilon;
}

#endif