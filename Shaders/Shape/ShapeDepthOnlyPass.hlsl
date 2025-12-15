#ifndef RAYMAN_SHAPE_DEPTHONLY
#define RAYMAN_SHAPE_DEPTHONLY

#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/BVH.hlsl"

struct Attributes
{
    float4 vertex : POSITION;
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
    float3 cameraPos = _WorldSpaceCameraPos;
    half3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.posWS);
    Ray ray = CreateRay(input.posWS, -viewDirWS, _EpsilonMin);
    ray.distanceTravelled = length(ray.hitPoint - cameraPos);
	
    shapeHitCount = TraverseBvh(_ShapeNodeBuffer,0, ray.origin, ray.dir, shapeHitIds).x;
    if (shapeHitCount == 0) discard;
    
    InsertionSort(shapeHitIds, shapeHitCount);
    if (!Raymarch(ray, _MaxSteps, _MaxDistance, float2(_EpsilonMin, _EpsilonMax))) discard;
    
    float lengthToSurface = length(input.posWS - cameraPos);
    const float depth = ray.distanceTravelled - lengthToSurface < ray.epsilon ?
        GetDepth(input.posWS) : GetDepth(ray.hitPoint);

    FragOut output;
    output.color = output.depth = depth;
    return output;
}

#endif