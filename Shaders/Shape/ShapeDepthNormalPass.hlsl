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
int _MaxSteps;
float _MaxDistance;

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
    
    float3 cameraPosWS = _WorldSpaceCameraPos;
    half3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.posWS);
    
    Ray ray = CreateRay(input.posWS, -viewDirWS, _EpsilonMin);
    ray.distanceTravelled = length(ray.hitPoint - cameraPosWS);
	
    shapeHitCount = TraverseBvh(_ShapeNodeBuffer,0, ray.origin, ray.dir, shapeHitIds).x;
    if (shapeHitCount == 0) discard;
    
    InsertionSort(shapeHitIds, shapeHitCount);
    if (!Raymarch(ray, _MaxSteps, _MaxDistance, float2(_EpsilonMin, _EpsilonMax))) discard;
    
    const float3 normal = GetNormal(ray.hitPoint, ray.epsilon);
    const float depth = GetNonLinearDepth(ray.hitPoint);

    FragOut output;
    output.normal = float4(normal, 0);
    output.depth = depth;
    return output;
}

#endif