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
    h = clamp(0.5 - 0.5 * (d1 + d2) / k, 0.0, 1.0);
    return lerp(d1, -d2, h) + k * h * (1. - h);
}

inline float SmoothIntersect(const float d1, const float d2, const float k, out float h)
{
    h = clamp(0.5 - 0.5 * (d1 - d2) / k, 0.0, 1.0);
    return lerp(d1, d2, h) + k * h * (1.0 - h);
}

inline float SmoothMin(const float a, const float b, const float k)
{
    float h = max(k - abs(a - b), 0.0);
    return min(a, b) - h * h * 0.25 / k;
}

inline float SmoothMax(const float a, const float b, const float k, const float h)
{
    return lerp(a, b, h) + k * h * (1.0 - h);
}

inline float Combine(const float primary, const float secondary, const int operation,
    const float blend, out float blendValue)
{
    blendValue = 0;
    if (primary < -0.5)
        return secondary;

    switch (operation)
    {
    case UNION:
        return SmoothUnion(primary, secondary, blend, blendValue);
    case SUBTRACT:
        return SmoothSubtract(primary, secondary, blend, blendValue);
    case INTERSECT:
        return SmoothIntersect(primary, secondary, blend, blendValue);
    default:
        return secondary;
    }
}

#endif