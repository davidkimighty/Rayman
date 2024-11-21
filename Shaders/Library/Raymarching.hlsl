#ifndef RAYMAN_RAYMARCHING
#define RAYMAN_RAYMARCHING

#include "Packages/com.davidkimighty.rayman/Shaders/Library/Math.hlsl"
#include "Packages/com.davidkimighty.rayman/Shaders/Library/Camera.hlsl"
#include "Packages/com.davidkimighty.rayman/Shaders/Library/TransformSpace.hlsl"
#include "Packages/com.davidkimighty.rayman/Shaders/Library/Shapes.hlsl"
#include "Packages/com.davidkimighty.rayman/Shaders/Library/Operations.hlsl"

struct Ray
{
    float3 origin;
    float3 dir;
    int maxSteps;
    float maxDist;
    float currentDist;
    float distTravelled;
    float3 travelledPoint;
};

inline Ray InitRay(const float3 origin, const int maxSteps, const float maxDist)
{
    Ray ray = (Ray)0;
    ray.origin = origin;
    ray.dir = normalize(origin - GetCameraPosition());
    ray.maxSteps = maxSteps;
    ray.maxDist = maxDist;
    ray.currentDist = 0.;
    ray.travelledPoint = ray.origin;
    ray.distTravelled = length(ray.travelledPoint - GetCameraPosition());
    return ray;
}

inline float Map(const float3 rayPos)
{
    Shape shape = _ShapeBuffer[0];
    float3 pos = GetShapePosition(rayPos, shape.transform);
#ifdef _OPERATION_FEATURE
    if (shape.operationEnabled > 0)
        ApplyOperationPositionById(pos, 0);
#endif
    float totalDist = GetShapeDistance(pos, shape.type, shape.size, shape.roundness);

    for (int i = 1; i < _ShapeCount; i++)
    {
        shape = _ShapeBuffer[i];
        pos = GetShapePosition(rayPos, shape.transform);
#ifdef _OPERATION_FEATURE
        if (shape.operationEnabled > 0)
            ApplyOperationPositionById(pos, i);
#endif
        float dist = GetShapeDistance(pos, shape.type, shape.size, shape.roundness);
        float blend = 0.;
        totalDist = CombineShape(totalDist, dist, shape.combination, shape.smoothness, blend);
    }
    return totalDist;
}

inline float Map(const float3 rayPos, out half4 color)
{
    Shape shape = _ShapeBuffer[0];
    float3 pos = GetShapePosition(rayPos, shape.transform);
#ifdef _OPERATION_FEATURE
    if (shape.operationEnabled > 0)
        ApplyOperationPositionById(pos, 0);
#endif
    float totalDist = GetShapeDistance(pos, shape.type, shape.size, shape.roundness);
    color = shape.color + shape.emissionColor * shape.emissionIntensity;

    for (int i = 1; i < _ShapeCount; i++)
    {
        shape = _ShapeBuffer[i];
        pos = GetShapePosition(rayPos, shape.transform);
#ifdef _OPERATION_FEATURE
        if (shape.operationEnabled > 0)
            ApplyOperationPositionById(pos, i);
#endif
        float dist = GetShapeDistance(pos, shape.type, shape.size, shape.roundness);

        float blend = 0.;
        totalDist = CombineShape(totalDist, dist, shape.combination, shape.smoothness, blend);
        color = lerp(color, shape.color + shape.emissionColor * shape.emissionIntensity, blend);
    }
    return totalDist;
}

inline bool Raymarch(inout Ray ray)
{
    for (int i = 0; i < ray.maxSteps; i++)
    {
        ray.currentDist = Map(ray.travelledPoint) * GetScale();
        ray.distTravelled += ray.currentDist;
        ray.travelledPoint += ray.dir * ray.currentDist;
        if (ray.currentDist < Epsilon || ray.distTravelled > ray.maxDist) break;
    }
    return ray.currentDist < Epsilon;
}

inline bool Raymarch(inout Ray ray, out half4 color)
{
    for (int i = 0; i < ray.maxSteps; i++)
    {
        ray.currentDist = Map(ray.travelledPoint, color) * GetScale();
        ray.distTravelled += ray.currentDist;
        ray.travelledPoint += ray.dir * ray.currentDist;
        if (ray.currentDist < Epsilon || ray.distTravelled > ray.maxDist) break;
    }
    return ray.currentDist < Epsilon;
}

inline float GetDepth(const Ray ray, const float3 wsPos)
{
    float lengthToSurface = length(wsPos - GetCameraPosition());
    return ray.distTravelled - lengthToSurface < Epsilon ? GetDepth(wsPos) : GetDepth(ray.travelledPoint);
}

inline float3 GetNormal(float3 pos)
{
    float3 x = float3(Epsilon, 0, 0);
    float3 y = float3(0, Epsilon, 0);
    float3 z = float3(0, 0, Epsilon);

    float distX = Map(pos + x) - Map(pos - x);
    float distY = Map(pos + y) - Map(pos - y);
    float distZ = Map(pos + z) - Map(pos - z);
    return normalize(float3(distX, distY, distZ));
}

#endif
