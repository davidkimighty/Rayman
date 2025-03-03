#ifndef RAYMAN_SDF
#define RAYMAN_SDF

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

inline float2 SegmentSdf(float3 p, float3 a, float3 b)
{
    float3 pa = p - a, ba = b - a;
    float h = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
    return float2(length(pa - ba * h), h);
}

inline float Determinant(float2 a, float2 b)
{
    return a.x * b.y - a.y * b.x;
}

inline float3 ClosestPoint(float2 b0, float2 b1, float2 b2)
{
    float a = Determinant(b0, b2);
    float b = 2.0 * Determinant(b1, b0);
    float d = 2.0 * Determinant(b2, b1);
    float f = b * d - a * a;
    float2 d21 = b2 - b1;
    float2 d10 = b1 - b0;
    float2 d20 = b2 - b0;
    float2 gf = 2.0 * (b * d21 + d * d10 + a * d20);
    gf = float2(gf.y, -gf.x);
    float2 pp = -f * gf / dot(gf, gf);
    float2 d0p = b0 - pp;
    float2 ap = Determinant(d0p, d20);
    float bp = 2.0 * Determinant(d10, d0p);
    float t = clamp((ap + bp) / (2.0 * a + b + d), 0.0, 1.0);
    return float3(lerp(lerp(b0, b1, t), lerp(b1, b2, t), t), t);
}

inline float2 QuadraticBezierSdf(float3 p, float3 a, float3 b, float3 c, out float3 pos)
{
    float3 w = normalize(cross(c - b, a - b));
    float3 u = normalize(c - b);
    float3 v = normalize(cross(w, u));

    float2 a2 = float2(dot(a - b, u), dot(a - b, v));
    float2 b2 = float2(0.0, 0.0);
    float2 c2 = float2(dot(c - b, u), dot(c - b, v));
    float3 p3 = float3(dot(p - b, u), dot(p - b, v), dot(p - b, w));

    float3 cp = ClosestPoint(a2 - p3.xy, b2 - p3.xy, c2 - p3.xy);
    //pos = b + cp.x * u + cp.y * v;
    pos = b + cp.x * u + cp.y * v + cp.z * w;
    return float2(sqrt(dot(cp.xy, cp.xy) + p3.z * p3.z), cp.z);
}

#endif