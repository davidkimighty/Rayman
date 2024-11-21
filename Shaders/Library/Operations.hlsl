#ifndef RAYMAN_OPERATIONS
#define RAYMAN_OPERATIONS

#define UNION (0)
#define SUBTRACT (1)
#define INTERSECT (2)

struct Operation
{
    int id;
    int type;
    float amount;
};

int _OperationCount;
StructuredBuffer<Operation> _OperationBuffer;

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

#define TWIST (1)
#define BEND (2)

inline float3 Twist(float3 pos, float strength)
{
    const float c = cos(strength * pos.y);
    const float s = sin(strength * pos.y);
    const float2x2 m = float2x2(c, -s, s, c);
    const float3 q = float3(mul(m, pos.xz), pos.y);
    return q;
}

inline float3 Bend(float3 pos, float strength)
{
    const float c = cos(strength * pos.x);
    const float s = sin(strength * pos.x);
    const float2x2 m = float2x2(c, -s, s, c);
    float3 q = float3(mul(m, pos.xy), pos.z);
    return q;
}

inline float3 GetOperationPosition(const float3 pos, const int type, const float strength)
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

inline void ApplyOperationPositionById(inout float3 pos, const int id)
{
    for (int i = 0; i < _OperationCount; i++)
    {
        Operation o = _OperationBuffer[i];
        if (o.id != id) continue;
                
        pos = GetOperationPosition(pos, o.type, o.amount);
        break;
    }
}

#endif