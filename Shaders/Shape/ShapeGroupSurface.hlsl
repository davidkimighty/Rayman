#ifndef RAYMAN_SHAPE_GROUP_SURFACE
#define RAYMAN_SHAPE_GROUP_SURFACE

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
inline void InitBlend(const int passType, int index);
inline void PreShapeBlend(const int passType, int index, float3 position, inout float distance);
inline void PostShapeBlend(const int passType, int index, float3 position, float blend);
inline void PostGroupBlend(const int passType, float blend);
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
    float localDist = RAY_MAX_DISTANCE;
    int prevGroupIndex = _ShapeGroupBuffer[shapeHitIds[0]].groupIndex;
#ifdef SHAPE_BLENDING
    InitBlend(passType, shapeHitIds[0]);
#endif
    
    for (int i = 0; i < shapeHitCount; i++)
    {
        int shapeIndex = shapeHitIds[i];
        Shape shape = _ShapeGroupBuffer[shapeIndex];
        
        if (prevGroupIndex != shape.groupIndex)
        {
            Group group = _GroupBuffer[prevGroupIndex];
            float groupBlend = 0;
            totalDist = SmoothOperation(group.operation, totalDist, localDist, group.blend, groupBlend);
#ifdef SHAPE_BLENDING
            PostGroupBlend(passType, groupBlend);
#endif
            localDist = RAY_MAX_DISTANCE;
            prevGroupIndex = shape.groupIndex;
#ifdef SHAPE_BLENDING
            InitBlend(passType, shapeIndex);
#endif
        }
        
        float3 localPos = positionWS - shape.position;
        float shapeDist = GetShapeDistance(shape, localPos);
#ifdef SHAPE_BLENDING
        PreShapeBlend(passType, shapeIndex, localPos, shapeDist);
#endif
        float blend = 0;
        localDist = SmoothOperation(shape.operation, localDist, shapeDist, shape.blend, blend);
#ifdef SHAPE_BLENDING
        PostShapeBlend(passType, shapeIndex, localPos, blend);
#endif
    }
    
    Group group = _GroupBuffer[prevGroupIndex];
    float groupBlend = 0;
    totalDist = SmoothOperation(group.operation, totalDist, localDist, group.blend, groupBlend);
#ifdef SHAPE_BLENDING
    PostGroupBlend(passType, groupBlend);
#endif
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

float ShadowMap(const float3 positionWS)
{
    return GetSceneDistance(PASS_SHADOW, positionWS);
}

#endif