#ifndef RAYMAN_OPERATIONS
#define RAYMAN_OPERATIONS

#define UNION (0)
#define SUBTRACT (1)
#define INTERSECT (2)

inline float SmoothUnion(const float d1, const float d2, const float k, out float h)
{
    h = clamp(0.5 + 0.5 * (d2 - d1) / k, 0., 1.);
    return lerp(d2, d1, h) - k * h * (1. - h);
}

inline float SmoothSubtract(const float d1, const float d2, const float k, out float h)
{
    h = clamp(0.5 - 0.5 * (d2 + d1) / k, 0., 1.);
    return lerp(d2, -d1, h) + k * h * (1. - h);
}

inline float SmoothIntersect(const float d1, const float d2, const float k, out float h)
{
    h = clamp(0.5 - 0.5 * (d2 - d1) / k, 0., 1.);
    return lerp(d2, d1, h) + k * h * (1. - h);
}

inline float SmoothMin(const float a, const float b, float k)
{
    float h = max(k - abs(a - b), 0.);
    return min(a, b) - h * h * 0.25 / k;
}

inline float SmoothMax(const float a, const float b, float k)
{
    float h = clamp(0.5 + 0.5 * (b - a) / k, 0., 1.);
    return lerp(a, b, h) + k * h * (1. - h);
}

inline float CombineShape(const float primary, const float secondary, const int operation,
    const float smoothness, out float blend_value)
{
    blend_value = 0.;
    if (primary < -0.5)
        return secondary;

    switch (operation)
    {
        case UNION:
            return SmoothUnion(secondary, primary, smoothness, blend_value);
        case SUBTRACT:
            return SmoothSubtract(secondary, primary, smoothness, blend_value);
        case INTERSECT:
            return SmoothIntersect(secondary, primary, smoothness, blend_value);
        default:
            return secondary;
    }
}

#endif