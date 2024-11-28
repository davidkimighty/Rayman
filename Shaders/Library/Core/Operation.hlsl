#ifndef RAYMAN_OPERATION
#define RAYMAN_OPERATION

#define UNION (0)
#define SUBTRACT (1)
#define INTERSECT (2)

inline float SmoothUnion(const float d1, const float d2, const float k, out float h)
{
    h = clamp(0.5 + 0.5 * (d1 - d2) / k, 0.0, 1.0);
    return lerp(d1, d2, h) - k * h * (1. - h);
}

inline float SmoothSubtract(const float d1, const float d2, const float k, out float h)
{
    h = clamp(0.5 - 0.5 * (d1 + d2) / k, 0., 1.);
    return lerp(d1, -d2, h) + k * h * (1. - h);
}

inline float SmoothIntersect(const float d1, const float d2, const float k, out float h)
{
    h = clamp(0.5 - 0.5 * (d1 - d2) / k, 0., 1.);
    return lerp(d1, d2, h) + k * h * (1. - h);
}

inline float SmoothMin(const float a, const float b, const float k)
{
    float h = max(k - abs(a - b), 0.);
    return min(a, b) - h * h * 0.25 / k;
}

inline float SmoothMax(const float a, const float b, const float k, const float h)
{
    return lerp(a, b, h) + k * h * (1. - h);
}

inline float CombineShapes(const float primary, const float secondary, const int operation,
    const float smoothness, out float blendValue)
{
    if (primary < -0.5)
        return secondary;

    switch (operation)
    {
    case UNION:
        return SmoothUnion(primary, secondary, smoothness, blendValue);
    case SUBTRACT:
        return SmoothSubtract(primary, secondary, smoothness, blendValue);
    case INTERSECT:
        return SmoothIntersect(primary, secondary, smoothness, blendValue);
    default:
        return secondary;
    }
}

#define TWIST (1)
#define BEND (2)

inline float3 Twist(const float3 pos, const float strength)
{
    const float c = cos(strength * pos.y);
    const float s = sin(strength * pos.y);
    const float2x2 m = float2x2(c, -s, s, c);
    const float3 q = float3(mul(m, pos.xz), pos.y);
    return q;
}

inline float3 Bend(const float3 pos, const float strength)
{
    const float c = cos(strength * pos.x);
    const float s = sin(strength * pos.x);
    const float2x2 m = float2x2(c, -s, s, c);
    float3 q = float3(mul(m, pos.xy), pos.z);
    return q;
}

inline float3 ApplyOperation(const float3 pos, const int type, const float strength)
{
    switch (type)
    {
    case TWIST:
        return Twist(pos, strength);
    case BEND:
        return Bend(pos, strength);
    default:
        return pos;
    }
}

#endif