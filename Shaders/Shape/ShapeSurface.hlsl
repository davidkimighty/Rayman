#ifndef RAYMAN_SHAPE_SURFACE
#define RAYMAN_SHAPE_SURFACE

#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/Math.hlsl"
#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/Operation.hlsl"
#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/Raymarch.hlsl"
#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/RaymarchLighting.hlsl"
#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/RaymarchShadow.hlsl"
#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/BVH.hlsl"
#include "Packages/com.davidkimighty.rayman/Shaders/Shape/Shape.hlsl"

#define PASS_MAP 0
#define PASS_NORMAL 1
#define PASS_SHADOW 2

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
inline void InitBlend(const int passType, int index);
inline void PreShapeBlend(const int passType, int index, float3 position, inout float distance);
inline void PostShapeBlend(const int passType, int index, float3 position, float blend);
#endif

inline float GetShapeDistance(const Shape shape, inout float3 localPos)
{
    localPos = RotateWithQuaternion(localPos, shape.rotation);
    localPos /= shape.scale;
    localPos -= GetPivotOffset(shape.type, shape.pivot, shape.size);

    float uniformScale = max(max(shape.scale.x, shape.scale.y), shape.scale.z);
    return GetShapeSdf(localPos, shape.type, shape.size, shape.roundness) * uniformScale;
}

inline float GetSceneDistance(const int passType, const float3 positionWS)
{
    if (shapeHitCount == 0) return RAY_MAX_DISTANCE;
    
    float totalDist = RAY_MAX_DISTANCE;
#ifdef SHAPE_BLENDING
    InitBlend(passType, shapeHitIds[0]);
#endif

    for (int i = 0; i < shapeHitCount; i++)
    {
        int shapeIndex = shapeHitIds[i];
        Shape shape = _ShapeBuffer[shapeIndex];
        float3 localPos = positionWS - shape.position;
        float shapeDist = GetShapeDistance(shape, localPos);
#ifdef SHAPE_BLENDING
        PreShapeBlend(passType, shapeIndex, localPos, shapeDist);
#endif
        float blend = 0;
        totalDist = SmoothOperation(shape.operation, totalDist, shapeDist, shape.blend, blend);
#ifdef SHAPE_BLENDING
        PostShapeBlend(passType, shapeIndex, localPos, blend);
#endif
    }
    return totalDist;
}

inline float Map(inout Ray ray)
{
    return GetSceneDistance(PASS_MAP, ray.hitPoint);
}

inline float NormalMap(const float3 positionWS)
{
    return GetSceneDistance(PASS_NORMAL, positionWS);
}

inline float ShadowMap(const float3 positionWS)
{
    return GetSceneDistance(PASS_SHADOW, positionWS);
}

#endif