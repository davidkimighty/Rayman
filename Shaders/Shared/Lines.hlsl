#ifndef RAYMAN_LINE
#define RAYMAN_LINE

#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/SDF.hlsl"

#define MAX_POINTS 4
#define SEGMENT (0)
#define QUADRATIC_BEZIER (1)
#define CUBIC_BEZIER (2)

inline float ThickLine(float d, float n, float ra, float rb)
{
    return d - lerp(ra, rb, smoothstep(0.0, 1.0, n));
}

inline float2 GetLineSdf(float3 pos, int type, float3 points[MAX_POINTS])
{
    switch (type)
    {
        case SEGMENT:
            return SegmentSdf(pos, points[0], points[1]);
        case QUADRATIC_BEZIER:
            return QuadraticBezierSdf(pos, points[0], points[1], points[2], pos);
        case CUBIC_BEZIER:
            //return CubicBezierSdf(pos, points[0], points[1], points[2], points[3], pos);
        default:
            return SegmentSdf(pos, points[0], points[1]);
    }
}

#endif