#ifndef RAYMAN_SPLINE_SURFACE
#define RAYMAN_SPLINE_SURFACE

#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/Math.hlsl"
#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/Operation.hlsl"
#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/Raymarch.hlsl"
#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/RaymarchLighting.hlsl"
#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/RaymarchShadow.hlsl"
#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/BVH.hlsl"
#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/SDF.hlsl"

#define PASS_MAP 0
#define PASS_NORMAL 1
#define PASS_SHADOW 2

// #define SPLINE_BLENDING

struct Spline
{
    int knotStartIndex;
    int knotCount;
};

struct Knot
{
    float3 position;
    float3 tangentIn;
    float3 tangentOut;
    float radius;
    float blend;
    int splineIndex;
};

StructuredBuffer<Spline> _SplineBuffer;
StructuredBuffer<Knot> _KnotBuffer;
StructuredBuffer<NodeAabb> _NodeBuffer;

int hitCount;
int hitIds[RAY_MAX_HITS];

#ifdef SPLINE_BLENDING
inline void SplineBlend(const int passType, float blend);
#endif

inline float GetSceneDistance(const int passType, const float3 positionWS)
{
    if (hitCount == 0) return RAY_MAX_DISTANCE;
    
    float totalDist = RAY_MAX_DISTANCE;
    
    for (int i = 0; i < hitCount; i++)
    {
        Knot a = _KnotBuffer[hitIds[i]];
        Knot b = _KnotBuffer[hitIds[i] + 1];
        float2 segment = SegmentSdf(positionWS, a.position, b.position);
        float dist = ThickLine(segment.x, segment.y, a.radius, b.radius);
        float prevDist = totalDist;
        totalDist = SmoothMinCubicPolynomial(totalDist, dist, a.blend);
#ifdef SPLINE_BLENDING
        if (a.splineIndex != b.splineIndex || dist > prevDist) continue;

        Spline spline = _SplineBuffer[a.splineIndex];
        int localIndexA = hitIds[i] - spline.knotStartIndex;
        int segmentCount = spline.knotCount - 1;
        float segmentStep = 1.0 / segmentCount;
        float t = (float)localIndexA * segmentStep;
        float blendY = t + segment.y * segmentStep;
        SplineBlend(passType, blendY);
#endif
    }
    return totalDist;
}

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