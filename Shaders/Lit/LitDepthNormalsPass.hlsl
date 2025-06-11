#ifndef RAYMAN_LIT_DEPTHNORMAL
#define RAYMAN_LIT_DEPTHNORMAL

#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/BVH.hlsl"

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
    float3 cameraPos = _WorldSpaceCameraPos;
    half3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.posWS);
    Ray ray = CreateRay(input.posWS, -viewDirWS, _EpsilonMin);
    ray.distanceTravelled = length(ray.hitPoint - cameraPos);
	
    hitCount = TraverseBvh(0, ray.origin, ray.dir, hitIds);
    if (hitCount.x == 0) discard;
    
    InsertionSort(hitIds, hitCount.x);
    if (!Raymarch(ray, _MaxSteps, _MaxDistance, float2(_EpsilonMin, _EpsilonMax))) discard;
    
    const float3 normal = GetNormal(ray.hitPoint, ray.epsilon);
    float lengthToSurface = length(input.posWS - cameraPos);
    const float depth = ray.distanceTravelled - lengthToSurface < ray.epsilon ?
        GetDepth(input.posWS) : GetDepth(ray.hitPoint);

    FragOut output;
    output.normal = float4(normal, 0);
    output.depth = depth;
    return output;
}

#endif