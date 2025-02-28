#ifndef RAYMAN_SHAPE
#define RAYMAN_SHAPE

#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/SDF.hlsl"

#define SPHERE (0)
#define ELLIPSOID (1)
#define BOX (2)
#define OCTAHEDRON (3)
#define CAPSULE (4)
#define CYLINDER (5)
#define TORUS (6)
#define CAPPED_TORUS (7)
#define LINK (8)
#define CAPPED_CONE (9)

float GetShapeSdf(const float3 pos, const int type, const float3 size, const float roundness)
{
    switch (type)
    {
        case SPHERE:
            return SphereSdf(pos, size.x);
        case ELLIPSOID:
            return EllipsoidSdf(pos, size);
        case BOX:
            return BoxSdf(pos, size) - roundness;
        case OCTAHEDRON:
            return OctahedronSdf(pos, size.x) - roundness;
        case CAPSULE:
            return CapsuleSdf(pos, size.xy);
        case CYLINDER:
            return CylinderSdf(pos, size.xy) - roundness;
        case TORUS:
            return TorusSdf(pos, size.xy);
        case CAPPED_TORUS:
            return CappedTorusSdf(pos, size);
        case LINK:
            return LinkSdf(pos, size);
        case CAPPED_CONE:
            return CappedConeSdf(pos, size) - roundness;
        default:
            return SphereSdf(pos, size.x);
    }
}

float3 GetPivotOffset(const int type, const float3 pivot, const float3 size)
{
    switch (type)
    {
        case SPHERE:
        case OCTAHEDRON:
            return size.x * ((pivot - 0.5) * 2);
        case CAPSULE:
            return (pivot - 0.5) * (size.x + size.y);
        case TORUS:
            return (pivot - 0.5) * 2 * (size.x + size.y);
        case LINK:
            return (pivot - 0.5) * 2 * (size.x + size.y + size.z);
        case CAPPED_TORUS:
            return (pivot - 0.5) * 2 * (size.y + size.z);
        default:
            return size * ((pivot - 0.5) * 2);
    }
}

#endif