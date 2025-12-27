#ifndef RAYMAN_PRIMITIVE_SDF
#define RAYMAN_PRIMITIVE_SDF

// Inigo Quilez - distance functions

inline float SphereSdf(const float3 pos, const float radius)
{
    return length(pos) - radius;
}

inline float EllipsoidSdf(const float3 pos, const float3 size)
{
    float k0 = length(pos / size);
    float k1 = length(pos / (size * size));
    return k0 * (k0 - 1.0) / k1;
}

inline float BoxSdf(const float3 pos, const float3 size)
{
    const float3 q = abs(pos) - size;
    return length(max(q, 0.0)) + min(max(q.x, max(q.y, q.z)), 0.0);
}

inline float OctahedronSdf(float3 pos, const float size)
{
    pos = abs(pos);
    const float m = pos.x + pos.y + pos.z - size;
    float3 q;
    if (3.0 * pos.x < m)
        q = pos.xyz;
    else if( 3.0 * pos.y < m)
        q = pos.yzx;
    else if( 3.0 * pos.z < m)
        q = pos.zxy;
    else return m * 0.57735027;
    
    const float k = clamp(0.5 * (q.z - q.y + size), 0.0, size); 
    return length(float3(q.x, q.y - size + k, q.z - k));
}

inline float CapsuleSdf(float3 pos, const float2 size)
{
    pos.y += size.y * 0.5;
    pos.y -= clamp(pos.y, 0.0, size.y);
    return length(pos) - size.x;
}

inline float CylinderSdf(const float3 pos, const float2 size)
{
    const float2 d = abs(float2(length(pos.xz), pos.y)) - size;
    return min(max(d.x, d.y), 0.0) + length(max(d, 0.0));
}

inline float TorusSdf(const float3 pos, const float2 size)
{
    const float2 q = float2(length(pos.xy) - size.x, pos.z);
    return length(q) - size.y;
}

inline float CappedTorusSdf(float3 pos, const float3 size)
{
    const float2 sc = float2(sin(size.x), cos(size.x));
    pos.x = abs(pos.x);
    const float k = (sc.y * pos.x > sc.x * pos.y) ? dot(pos.xy, sc) : length(pos.xy);
    return sqrt(dot(pos, pos) + size.y * size.y - 2.0 * size.y * k) - size.z;
}

inline float LinkSdf(const float3 pos, const float3 size)
{
    const float3 q = float3(pos.x, max(abs(pos.y) - size.y, 0.0), pos.z);
    return length(float2(length(q.xy) - size.x, q.z)) - size.z;
}

inline float ConeSdf(const float3 pos, const float3 size)
{
    const float q = length(pos.xz);
    return max(dot(size.zx, float2(q, pos.y)), -size.y - pos.y);
}

inline float CappedConeSdf(const float3 pos, const float3 size)
{
    const float2 q = float2(length(pos.xz), pos.y);
    const float2 k1 = float2(size.z, size.y);
    const float2 k2 = float2(size.z - size.x, 2.0 * size.y);
    
    const float2 ca = float2(q.x - min(q.x, (q.y < 0.0) ? size.x : size.z), abs(q.y) - size.y);
    const float2 cb = q - k1 + k2 * clamp(dot(k1 - q, k2) / dot(k2, k2), 0.0, 1.0);
    
    const float s = (cb.x < 0.0 && ca.y < 0.0) ? -1.0 : 1.0;
    return s * sqrt(min(dot(ca, ca), dot(cb, cb)));
}

#endif