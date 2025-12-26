#ifndef RAYMAN_SHAPE_DEPTHNORMAL
#define RAYMAN_SHAPE_DEPTHNORMAL

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.davidkimighty.rayman/Shaders/Library/Geometry.hlsl"

struct Attributes
{
    float4 vertex : POSITION;
    float3 normal : NORMAL;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 posCS : SV_POSITION;
    float3 posWS : TEXCOORD0;
    float3 normalWS : TEXCOORD1;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

struct FragOut
{
    float4 normal : SV_Target;
    float depth : SV_Depth;
};

float _EpsilonMin;
float _EpsilonMax;
int _DepthNormalMaxSteps;
float _DepthNormalMaxDistance;

Varyings Vert(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    
    output.posCS = TransformObjectToHClip(input.vertex.xyz);
    output.posWS = TransformObjectToWorld(input.vertex.xyz);
    output.normalWS = TransformObjectToWorldNormal(input.normal);
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
    if (!Raymarch(ray, _DepthNormalMaxSteps, _DepthNormalMaxDistance, _EpsilonMin, _EpsilonMax)) discard;
    
    const float3 normal = GetNormal(ray.hitPoint, _EpsilonMin);
    const float depth = GetNonLinearDepth(ray.hitPoint);

    FragOut output;
    output.normal = float4(normal, 0);
    output.depth = depth;
    return output;
}

#endif