#ifndef RAYMAN_SHAPE_SURFACE
#define RAYMAN_SHAPE_SURFACE

#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/Math.hlsl"
#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/Operation.hlsl"
#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/Raymarch.hlsl"
#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/RaymarchLighting.hlsl"
#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/RaymarchShadow.hlsl"
#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/Bvh.hlsl"
#include "Packages/com.davidkimighty.rayman/Shaders/Sdf/Shape.hlsl"

#define PASS_MAP 0
#define PASS_NORMAL 1
#define PASS_SHADOW 2

//#define SHAPE_BLENDING
//#define _SHAPE_GROUP

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
#ifdef _SHAPE_GROUP
    int groupIndex;
#endif
};

#ifdef _SHAPE_GROUP
struct Group
{
    int operation;
    float blend;
};
#endif

struct BlendParams
{
    int index;
    float3 pos;
    half3 size;
    float blend;
};

StructuredBuffer<Shape> _ShapeBuffer;
StructuredBuffer<NodeAabb> _NodeBuffer;
#ifdef _SHAPE_GROUP
StructuredBuffer<Group> _GroupBuffer;
#endif

int hitCount;
int hitIds[RAY_MAX_HITS];

#ifdef SHAPE_BLENDING
inline void PreShapeBlend(const int passType, BlendParams params, inout float shapeDistance);
inline void PostShapeBlend(const int passType, BlendParams params, inout float combinedDistance);
#ifdef _SHAPE_GROUP
inline void PostGroupBlend(const int passType, float blend);
#endif
#endif

inline float GetShapeDistance(const Shape shape, inout float3 localPos)
{
    localPos = RotateWithQuaternion(localPos, shape.rotation);
    localPos /= shape.scale;
    localPos -= GetPivotOffset(shape.type, shape.pivot, shape.size);

    float uniformScale = min(shape.scale.x, min(shape.scale.y, shape.scale.z));
    return GetShapeSdf(localPos, shape.type, shape.size, shape.roundness) * uniformScale;
}

#ifndef _SHAPE_GROUP
inline float GetSceneDistance(const int passType, const float3 positionWS)
{
    if (hitCount == 0) return RAY_MAX_DISTANCE;
    
    float totalDist = RAY_MAX_DISTANCE;

    for (int i = 0; i < hitCount; i++)
    {
        int shapeIndex = hitIds[i];
        Shape shape = _ShapeBuffer[shapeIndex];
        float3 localPos = positionWS - shape.position;
        float shapeDist = GetShapeDistance(shape, localPos);
#ifdef SHAPE_BLENDING
        BlendParams params = { shapeIndex, localPos, shape.size, 0};
        PreShapeBlend(passType, params, shapeDist);
        totalDist = SmoothOperation(shape.operation, totalDist, shapeDist, shape.blend, params.blend);
        PostShapeBlend(passType, params, totalDist);
#else
        float blend = 0;
        totalDist = SmoothOperation(shape.operation, totalDist, shapeDist, shape.blend, blend);
#endif
    }
    return totalDist;
}
#else
inline float GetSceneDistance(const int passType, const float3 positionWS)
{
    if (hitCount == 0) return RAY_MAX_DISTANCE;
    
    float totalDist = RAY_MAX_DISTANCE;
    float localDist = RAY_MAX_DISTANCE;
    int prevGroupIndex = _ShapeBuffer[hitIds[0]].groupIndex;
    
    for (int i = 0; i < hitCount; i++)
    {
        int shapeIndex = hitIds[i];
        Shape shape = _ShapeBuffer[shapeIndex];
        
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
        }
        
        float3 localPos = positionWS - shape.position;
        float shapeDist = GetShapeDistance(shape, localPos);
#ifdef SHAPE_BLENDING
        BlendParams params = { shapeIndex, localPos, shape.size, 0};
        PreShapeBlend(passType, params, shapeDist);
        localDist = SmoothOperation(shape.operation, localDist, shapeDist, shape.blend, params.blend);
        PostShapeBlend(passType, params, totalDist);
#else
        float blend = 0;
        localDist = SmoothOperation(shape.operation, localDist, shapeDist, shape.blend, blend);
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
#endif

inline float Map(const float3 positionWS)
{
    return GetSceneDistance(PASS_MAP, positionWS);
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