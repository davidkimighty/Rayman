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
            return Sphere(pos, size.x);
        case ELLIPSOID:
            return Ellipsoid(pos, size);
        case BOX:
            return Box(pos, size) - roundness;
        case OCTAHEDRON:
            return Octahedron(pos, size.x) - roundness;
        case CAPSULE:
            return Capsule(pos, size.xy);
        case CYLINDER:
            return Cylinder(pos, size.xy) - roundness;
        case TORUS:
            return Torus(pos, size.xy);
        case CAPPED_TORUS:
            return CappedTorus(pos, size);
        case LINK:
            return Link(pos, size);
        case CAPPED_CONE:
            return CappedCone(pos, size) - roundness;
        default:
            return Sphere(pos, size.x);
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