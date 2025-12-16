#ifndef RAYMAN_SHAPE_SURFACE
#define RAYMAN_SHAPE_SURFACE

#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/Math.hlsl"
#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/Operation.hlsl"
#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/Raymarch.hlsl"
#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/RaymarchLighting.hlsl"
#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/RaymarchShadow.hlsl"
#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/BVH.hlsl"
#include "Packages/com.davidkimighty.rayman/Shaders/Shape/Shape.hlsl"

struct Shape
{
    float3 position;
    float4 rotation;
    float3 scale;
    half3 size;
    half3 pivot;
    int operation;
    half blend;
    half roundness;
    int type;
};

StructuredBuffer<Shape> _ShapeBuffer;
StructuredBuffer<NodeAabb> _ShapeNodeBuffer;

int shapeHitCount;
int shapeHitIds[RAY_MAX_HITS];

#ifdef SHAPE_BLENDING
inline void PreBlend(int index);
inline void ShapeBlend(int index, float3 position, float blend);
#endif

float GetShapeDistance(Shape shape, float3 localPos)
{
    localPos = RotateWithQuaternion(localPos, shape.rotation);
    localPos /= shape.scale;
    localPos -= GetPivotOffset(shape.type, shape.pivot, shape.size);

    float uniformScale = max(max(shape.scale.x, shape.scale.y), shape.scale.z);
    return GetShapeSdf(localPos, shape.type, shape.size, shape.roundness) * uniformScale;
}

inline float GetSceneDistance(const float3 positionWS, const bool doBlend)
{
    if (shapeHitCount == 0) return RAY_MAX_DISTANCE;
    
    float totalDist = RAY_MAX_DISTANCE;
#ifdef SHAPE_BLENDING
    if (doBlend) PreBlend(shapeHitIds[0]);
#endif
			
    for (int i = 0; i < shapeHitCount; i++)
    {
        int shapeIndex = shapeHitIds[i];
        Shape shape = _ShapeBuffer[shapeIndex];
        float3 localPos = positionWS - shape.position;
        float shapeDist = GetShapeDistance(shape, localPos);
        float blend = 0;
        totalDist = SmoothOperation(shape.operation, totalDist, shapeDist, shape.blend, blend);
#ifdef SHAPE_BLENDING
        if (doBlend) ShapeBlend(shapeIndex, localPos, blend);
#endif
    }
    return totalDist;
}

inline float Map(inout Ray ray)
{
    return GetSceneDistance(ray.hitPoint, true);
}

inline float NormalMap(const float3 positionWS)
{
    return GetSceneDistance(positionWS, false);
}

inline float ShadowMap(const float3 positionWS)
{
    return GetSceneDistance(positionWS, false);
}

#endif