#ifndef RAYMAN_SHAPE_DEPTHONLY
#define RAYMAN_SHAPE_DEPTHONLY

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/Bvh.hlsl"
#include "Packages/com.davidkimighty.rayman/Shaders/Library/Geometry.hlsl"

struct Attributes
{
    float4 position : POSITION;
    float2 texcoord : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 posCS : SV_POSITION;
    float3 posWS : TEXCOORD0;
#if defined(_ALPHATEST_ON)
    float2 uv : TEXCOORD1;
#endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

struct FragOut
{
    float4 color : SV_Target;
    float depth : SV_Depth;
};

float _EpsilonMin;
float _EpsilonMax;
int _DepthOnlyMaxSteps;
float _DepthOnlyMaxDistance;

Varyings Vert(Attributes input)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    
    output.posCS = TransformObjectToHClip(input.position.xyz);
    output.posWS = TransformObjectToWorld(input.position.xyz);
#if defined(_ALPHATEST_ON)
    output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
#endif
    return output;
}

FragOut Frag(Varyings input)
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    
    half3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.posWS);
    Ray ray = CreateRay(input.posWS, -viewDirWS);

    hitCount = TraverseBvh(_NodeBuffer,0, ray.origin, ray.dir, hitIds);
    if (hitCount == 0) discard;
    
    InsertionSort(hitIds, hitCount);
    if (!Raymarch(ray, _DepthOnlyMaxSteps, _DepthOnlyMaxDistance, _EpsilonMin, _EpsilonMax)) discard;

#if defined(_ALPHATEST_ON)
    Alpha(SampleAlbedoAlpha(input.uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap)).a, _BaseColor, _Cutoff);
#endif
    
    FragOut output;
    output.color = output.depth = GetNonLinearDepth(ray.hitPoint);
    return output;
}

#endif