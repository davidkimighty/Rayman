#ifndef RAYMAN_SPLINE_SURFACE
#define RAYMAN_SPLINE_SURFACE

#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/Operation.hlsl"
#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/Raymarch.hlsl"
#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/RaymarchLighting.hlsl"
#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/RaymarchShadow.hlsl"
#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/Bvh.hlsl"
#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/SplineSdf.hlsl"

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
    int splineIndex;
};

int _BezierSubdiv;

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

        float3 p0 = a.position;
        float3 p1 = a.position + a.tangentOut;
        float3 p2 = b.position + b.tangentIn;
        float3 p3 = b.position;

        float bezierT;
        float dist = CubicBezierSegmentSdf(positionWS, p0, p1, p2, p3, bezierT, _BezierSubdiv);
        dist = ThickLine(dist, bezierT, a.radius, b.radius);
        
        float prevDist = totalDist;
        totalDist = min(totalDist, dist);
        
#ifdef SPLINE_BLENDING
        if (a.splineIndex != b.splineIndex || dist > prevDist) continue;

        Spline spline = _SplineBuffer[a.splineIndex];
        int localIndexA = hitIds[i] - spline.knotStartIndex;
        float segmentStep = 1.0 / (spline.knotCount - 1);
        float t = (float)localIndexA * segmentStep;
        float blendY = t + bezierT * segmentStep;
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