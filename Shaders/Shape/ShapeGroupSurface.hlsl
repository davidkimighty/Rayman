#ifndef RAYMAN_SHAPE_GROUP_SURFACE
#define RAYMAN_SHAPE_GROUP_SURFACE

#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/Math.hlsl"
#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/Operation.hlsl"
#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/Raymarch.hlsl"
#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/RaymarchLighting.hlsl"
#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/BVH.hlsl"
#include "Packages/com.davidkimighty.rayman/Shaders/Shape/Shape.hlsl"

struct Group
{
    int operation;
    float blend;
};

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
    int groupIndex;
};

StructuredBuffer<Group> _GroupBuffer;
StructuredBuffer<Shape> _ShapeGroupBuffer;
StructuredBuffer<NodeAabb> _ShapeNodeBuffer;

int shapeHitCount;
int shapeHitIds[RAY_MAX_HITS];

#ifdef SHAPE_BLENDING
inline void PreBlend(int index);
inline void ShapeBlend(int index, float3 position, float blend);
inline void GroupBlend(float blend);
#endif

float GetShapeDistance(Shape shape, float3 localPos)
{
    localPos = RotateWithQuaternion(localPos, shape.rotation);
    localPos /= shape.scale;
    localPos -= GetPivotOffset(shape.type, shape.pivot, shape.size);

    float uniformScale = max(max(shape.scale.x, shape.scale.y), shape.scale.z);
    return GetShapeSdf(localPos, shape.type, shape.size, shape.roundness) * uniformScale;
}

inline float GroupBlending(Group group, float totalDist, float localDist, bool doBlend)
{
    float groupBlend = 0;
    float total = SmoothOperation(group.operation, totalDist, localDist, group.blend, groupBlend);
#ifdef SHAPE_BLENDING
    if (doBlend) GroupBlend(groupBlend);
#endif
    return total;
}

inline float GetSceneDistance(const float3 positionWS, const bool doBlend)
{
    if (shapeHitCount == 0) return RAY_MAX_DISTANCE;
    
    float totalDist = RAY_MAX_DISTANCE;
    float localDist = RAY_MAX_DISTANCE;
    int groupIndex = _ShapeGroupBuffer[shapeHitIds[0]].groupIndex;
#ifdef SHAPE_BLENDING
    if (doBlend) PreBlend(shapeHitIds[0]);
#endif
    
    for (int i = 0; i < shapeHitCount; i++)
    {
        int shapeIndex = shapeHitIds[i];
        Shape shape = _ShapeGroupBuffer[shapeIndex];

        if (groupIndex != shape.groupIndex)
        {
            totalDist = GroupBlending(_GroupBuffer[groupIndex], totalDist, localDist, doBlend);
            localDist = RAY_MAX_DISTANCE;
            groupIndex = shape.groupIndex;
#ifdef SHAPE_BLENDING
            if (doBlend) PreBlend(shapeIndex);
#endif
        }
        float3 localPos = positionWS - shape.position;
        float shapeDist = GetShapeDistance(shape, localPos);
        float blend = 0;
        localDist = SmoothOperation(shape.operation, localDist, shapeDist, shape.blend, blend);
#ifdef SHAPE_BLENDING
        if (doBlend) ShapeBlend(shapeIndex, localPos, blend);
#endif
    }
    totalDist = GroupBlending(_GroupBuffer[groupIndex], totalDist, localDist, doBlend);
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

#endif