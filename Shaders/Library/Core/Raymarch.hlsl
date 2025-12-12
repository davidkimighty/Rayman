#ifndef RAYMAN_RAYMARCH
#define RAYMAN_RAYMARCH

#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/Ray.hlsl"

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

#endif