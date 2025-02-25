#ifndef RAYMAN_LINE
#define RAYMAN_LINE

#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/SDF.hlsl"

#define MAX_POINTS 4
#define SEGMENT (0)
#define QUADRATIC_BEZIER (1)

inline float ThickLine(float d, float n, float ra, float rb)
{
    return d - lerp(ra, rb, smoothstep(0.0, 1.0, n));
}

inline float2 GetLineSdf(float3 pos, int type, float3 points[MAX_POINTS])
{
    switch (type)
    {
        case SEGMENT:
            return Segment(pos, points[0], points[1]);
        case QUADRATIC_BEZIER:
            return QuadraticBezier(pos, points[0], points[1], points[2], pos);
        default:
            return Segment(pos, points[0], points[1]);
    }
}

#endif