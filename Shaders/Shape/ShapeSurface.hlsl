#ifndef RAYMAN_SHAPE_SURFACE
#define RAYMAN_SHAPE_SURFACE

#include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
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
    int startIndex;
    int count;
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
};

StructuredBuffer<Group> _GroupBuffer;
StructuredBuffer<Shape> _ShapeBuffer;
StructuredBuffer<NodeAabb> _NodeBuffer;

CBUFFER_START(RaymarchPerGroup)
float _EpsilonMin;
float _EpsilonMax;
int _MaxSteps;
float _MaxDistance;
int _ShadowMaxSteps;
float _ShadowMaxDistance;
CBUFFER_END

int hitCount;
int hitIds[RAY_MAX_HITS];

#ifdef SHAPE_BLENDING
void PreBlend(int index);
void LocalBlend(int index, float3 position, float blend);
void GroupBlend(float blend);
#endif

float GetShapeDistance(Shape shape, float3 localPos)
{
    localPos = RotateWithQuaternion(localPos, shape.rotation);
    localPos /= shape.scale;
    localPos -= GetPivotOffset(shape.type, shape.pivot, shape.size);

    float uniformScale = max(max(shape.scale.x, shape.scale.y), shape.scale.z);
    return GetShapeSdf(localPos, shape.type, shape.size, shape.roundness) * uniformScale;
}

inline float CombineGroupDistance(const float3 positionWS)
{
    float totalDist = _MaxDistance;
    for (int i = 0; i < hitCount; i++)
    {
        float localDist = _MaxDistance;
        Group group = _GroupBuffer[hitIds[i]];
        int index = group.startIndex;
#ifdef SHAPE_BLENDING
        PreBlend(index);
#endif
        for (int j = 0; j < group.count; j++)
        {
            Shape shape = _ShapeBuffer[index];
            float3 localPos = positionWS - shape.position;
            float shapeDist = GetShapeDistance(shape, localPos);
            float blend = 0;
            localDist = SmoothOperation(shape.operation, localDist, shapeDist, shape.blend, blend);
#ifdef SHAPE_BLENDING
            LocalBlend(index, localPos, blend);
#endif
            index++;
        }
        float groupBlend = 0;
        totalDist = SmoothOperation(group.operation, totalDist, localDist, group.blend, groupBlend);
#ifdef SHAPE_BLENDING
        GroupBlend(groupBlend);
#endif
    }
    return totalDist;
}

float Map(inout Ray ray)
{
    return CombineGroupDistance(ray.hitPoint);
}

float NormalMap(const float3 positionWS)
{
    return CombineGroupDistance(positionWS);
}

inline NodeAabb GetNode(const int index)
{
    return _NodeBuffer[index];
}

#endif