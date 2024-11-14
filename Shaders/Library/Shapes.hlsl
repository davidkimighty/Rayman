#ifndef RAYMAN_SHAPES
#define RAYMAN_SHAPES

#define SPHERE (0)
#define BOX (1)
#define CAPSULE (2)
#define CYLINDER (3)
#define TORUS (4)
#define LINK (5)
#define PLANE (6)
#define CAPPED_TORUS (7)
#define LINK (8)
#define CONE (9)

struct Shape
{
    float4x4 transform;
    int type;
    float3 size;
    int operation;
    float smoothness;
    half4 color;
    half4 emissionColor;
    float emissionIntensity;
};

int _ShapeCount;
StructuredBuffer<Shape> _ShapeBuffer;

inline float SdfSphere(const float3 pos, const float radius)
{
    return length(pos) - radius;
}

inline float SdfBox(const float3 pos, const float3 size)
{
    const float3 q = abs(pos) - size;
    return length(max(q, 0.)) + min(max(q.x, max(q.y, q.z)), 0.);
}

inline float SdfCapsule(float3 pos, const float2 size)
{
    pos.y += size.x * 0.5;
    pos.y -= clamp(pos.y, 0., size.x);
    return length(pos) - size.y;
}

inline float SdfCylinder(const float3 pos, const float2 size)
{
    const float2 d = abs(float2(length(pos.xz), pos.y)) - float2(size.y, size.x);
    return min(max(d.x, d.y), 0.) + length(max(d, 0.));
}

inline float SdfTorus(const float3 pos, const float2 size)
{
    const float2 q = float2(length(pos.xz) - size.x, pos.y);
    return length(q) - size.y;
}

inline float SdfLink(const float3 pos, const float3 size)
{
    const float3 q = float3(pos.x, max(abs(pos.y) - size.x, 0.), pos.z);
    return length(float2(length(q.xy) - size.y, q.z)) - size.z;
}

inline float SdfCappedTorus(float3 pos, const float3 size)
{
    const float2 sc = float2(sin(size.x), cos(size.x));
    pos.x = abs(pos.x);
    const float k = (sc.y * pos.x > sc.x * pos.y) ? dot(pos.xy, sc) : length(pos.xy);
    return sqrt(dot(pos, pos) + size.y * size.y - 2. * size.y * k) - size.z;
}

inline float SdfPlane(const float3 pos)
{
    return pos.y;
}

inline float SdfCone(const float3 pos, const float3 size)
{
    const float q = length(pos.xz);
    return max(dot(size.xy, float2(q, pos.y)), -size.z - pos.y);
}

inline float GetShapeDistance(const float3 pos, const int type, const float3 size)
{
    float dist;
    switch (type)
    {
        case SPHERE:
            dist = SdfSphere(pos, size.x);
            break;
        case BOX:
            dist = SdfBox(pos, size);
            break;
        case CAPSULE:
            dist = SdfCapsule(pos, size.xy);
            break;
        case CYLINDER:
            dist = SdfCylinder(pos, size.xy);
            break;
        case TORUS:
            dist = SdfTorus(pos, size.xy);
            break;
        case LINK:
            dist = SdfLink(pos, size);
            break;
        case CAPPED_TORUS:
            dist = SdfCappedTorus(pos, size);
            break;
        case PLANE:
            dist = SdfPlane(pos);
            break;
        case CONE:
            dist = SdfCone(pos, size);
            break;
        default:
            dist = SdfSphere(pos, size.x);
            break;
    }
    return dist;
}

inline float3 GetShapePosition(const float3 pos, const float4x4 transform)
{
    return mul(transform, float4(pos, 1.));
}

#endif