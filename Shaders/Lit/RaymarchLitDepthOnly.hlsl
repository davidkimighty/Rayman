#ifndef RAYMAN_DEPTHONLY
#define RAYMAN_DEPTHONLY

#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/BVH.hlsl"
#include "Packages/com.davidkimighty.rayman/Shaders/Library/Camera.hlsl"
#include "Packages/com.davidkimighty.rayman/Shaders/Library/Geometry.hlsl"

struct Attributes
{
    float4 vertex : POSITION;
    float3 normal : NORMAL;
};

struct Varyings
{
    float4 posCS : SV_POSITION;
    float3 posWS : TEXCOORD0;
};

struct FragOut
{
    float4 color : SV_Target;
    float depth : SV_Depth;
};

Varyings Vert(Attributes input)
{
    Varyings output;
    output.posCS = TransformObjectToHClip(input.vertex.xyz);
    output.posWS = TransformObjectToWorld(input.vertex.xyz);
    return output;
}

FragOut Frag(Varyings input)
{
    const float3 cameraPos = GetCameraPosition();
    const float3 rayDir = normalize(input.posWS - cameraPos);
    Ray ray = CreateRay(input.posWS, rayDir, _MaxSteps, _MaxDist);
    ray.travelDistance = length(ray.hitPoint - cameraPos);
	
    TraverseAabbTree(ray, hitIds, hitCount);
    InsertionSort(hitIds, hitCount.x);
	
    if (!Raymarch(ray)) discard;
	
    float lengthToSurface = length(input.posWS - cameraPos);
    const float depth = ray.travelDistance - lengthToSurface < EPSILON ?
        GetDepth(input.posWS) : GetDepth(ray.hitPoint);

    FragOut output;
    output.color =  output.depth = depth;
    return output;
}

#endif