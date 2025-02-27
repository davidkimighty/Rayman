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
    const float3 cameraPos = GetCameraPosition();
    const float3 rayDir = normalize(input.posWS - cameraPos);
    Ray ray = CreateRay(input.posWS, rayDir, float2(_EpsilonMin, _EpsilonMax), _MaxSteps, _MaxDistance);
    ray.distanceTravelled = length(ray.hitPoint - cameraPos);
	
    hitCount = GetHitIds(0, ray, hitIds);
    InsertionSort(hitIds, hitCount.x);
	
    if (!Raymarch(ray)) discard;
    
    const float3 normal = GetNormal(ray.hitPoint, ray.epsilon.z);
    float lengthToSurface = length(input.posWS - cameraPos);
    const float depth = ray.distanceTravelled - lengthToSurface < ray.epsilon.z ?
        GetDepth(input.posWS) : GetDepth(ray.hitPoint);

    FragOut output;
    output.normal = float4(normal, 0);
    output.depth = depth;
    return output;
}

#endif