#ifndef RAYMAN_OPERATION
#define RAYMAN_OPERATION

#define UNION (0)
#define SUBTRACT (1)
#define INTERSECT (2)

// Inigo Quilez - smooth min functions

inline float NormalizeBlend(float a, float b, float k)
{
    return max(k - abs(a - b), 0.0) / max(k, 1e-8f);
}

inline float SmoothMinQuadraticPolynomial(float a, float b, float k)
{
    k *= 4.0;
    float h = NormalizeBlend(a, b, k);
    return min(a, b) - h * h * k * (1.0 / 4.0);
}

inline float SmoothMinCubicPolynomial(float a, float b, float k)
{
    k *= 6.0;
    float h = NormalizeBlend(a, b, k);
    return min(a, b) - h * h * h * k * (1.0 / 6.0);
}

inline float SmoothMinQuarticPolynomial(float a, float b, float k)
{
    k *= 16.0 / 3.0;
    float h = NormalizeBlend(a, b, k);
    return min(a, b) - h * h * h * (4.0 - h) * k * (1.0 / 16.0);
}

inline float SmoothMinCircular(float a, float b, float k)
{
    k *= 1.0 / (1.0 - sqrt(0.5));
    float h = NormalizeBlend(a, b, k);
    return min(a, b) - k * 0.5 * (1.0 + h - sqrt(1.0 - h * (h - 2.0)));
}

inline float SmoothMin(const float a, const float b, const float k)
{
    float h = NormalizeBlend(a, b, k);
    return min(a, b) - h * h * k * 0.25;
}

inline float SmoothMax(const float a, const float b, const float k)
{
    float h = NormalizeBlend(a, b, k);
    return max(a, b) + h * h * k * 0.25;
}

inline float2 SmoothOperation(const int operation, const float a, const float b, const float k)
{
    if (a < -0.5)
        return b;

    float colorBlend;
    switch (operation)
    {
        case UNION:
            colorBlend = clamp(0.5 + 0.5 * (a - b) / k, 0.0, 1.0);
            return float2(SmoothMin(a, b, k), colorBlend);
        case SUBTRACT:
            colorBlend = clamp(0.5 - 0.5 * (a + b) / k, 0.0, 1.0);
            return float2(SmoothMax(a, -b, k), colorBlend);
        case INTERSECT:
            colorBlend = clamp(0.5 - 0.5 * (a - b) / k, 0.0, 1.0);
            return float2(-SmoothMin(-a, -b, k), colorBlend);
        default:
            return b;
    }
}

#endif