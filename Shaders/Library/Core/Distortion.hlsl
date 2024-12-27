#ifndef RAYMAN_DISTORTION
#define RAYMAN_DISTORTION

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

inline float3 ApplyDistortion(const float3 pos, const int type, const float strength)
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