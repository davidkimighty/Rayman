#ifndef RAYMAN_DEPTHNORMAL
#define RAYMAN_DEPTHNORMAL

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
    float3 normalWS : TEXCOORD1;
};

struct FragOut
{
    float4 normal : SV_Target;
    float depth : SV_Depth;
};

Varyings Vert(Attributes input)
{
    Varyings output;
    output.posCS = TransformObjectToHClip(input.vertex.xyz);
    output.posWS = TransformObjectToWorld(input.vertex.xyz);
    output.normalWS = TransformObjectToWorldNormal(input.normal);
    return output;
}

FragOut Frag(Varyings input)
{
    const float3 cameraPos = GetCameraPosition();
    const float3 rayDir = normalize(input.posWS - cameraPos);
    Ray ray = CreateRay(input.posWS, rayDir, _MaxSteps, _MaxDistance);
    ray.distanceTravelled = length(ray.hitPoint - cameraPos);
	
    TraverseTree(0, ray, hitIds, hitCount);
    InsertionSort(hitIds, hitCount.x);
	
    if (!Raymarch(ray)) discard;
    
    const float3 normal = GetNormal(ray.hitPoint);
    float lengthToSurface = length(input.posWS - cameraPos);
    const float depth = ray.distanceTravelled - lengthToSurface < EPSILON ?
        GetDepth(input.posWS) : GetDepth(ray.hitPoint);

    FragOut output;
    output.normal = float4(normal, 0);
    output.depth = depth;
    return output;
}

#endif