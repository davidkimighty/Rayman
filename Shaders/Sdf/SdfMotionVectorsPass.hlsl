#ifndef RAYMAN_SHAPE_MOTIONVECTORS
#define RAYMAN_SHAPE_MOTIONVECTORS

#pragma target 3.5

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityInput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MotionVectorsCommon.hlsl"
#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/Bvh.hlsl"

struct Attributes
{
    float4 position : POSITION;
#if _ALPHATEST_ON
    float2 uv : TEXCOORD0;
#endif
    float3 positionOld : TEXCOORD4;
#if _ADD_PRECOMPUTED_VELOCITY
    float3 alembicMotionVector : TEXCOORD5;
#endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float4 positionCSNoJitter : POSITION_CS_NO_JITTER;
    float4 previousPositionCSNoJitter : PREV_POSITION_CS_NO_JITTER;
    float3 positionWS : TEXCOORD0;
#if _ALPHATEST_ON
    float2 uv : TEXCOORD1;
#endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

float _EpsilonMin;
float _EpsilonMax;
int _MotionVectorsMaxSteps;
float _MotionVectorsMaxDistance;

Varyings Vert(Attributes input)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    const VertexPositionInputs vertexInput = GetVertexPositionInputs(input.position.xyz);
#if defined(_ALPHATEST_ON)
    output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
#endif

#if defined(APPLICATION_SPACE_WARP_MOTION)
    output.positionCSNoJitter = mul(_NonJitteredViewProjMatrix, mul(UNITY_MATRIX_M, input.position));;
    output.positionCS = output.positionCSNoJitter;
#else
    output.positionCS = vertexInput.positionCS;
    output.positionCSNoJitter = mul(_NonJitteredViewProjMatrix, mul(UNITY_MATRIX_M, input.position));
#endif

    float4 prevPos = (unity_MotionVectorsParams.x == 1) ? float4(input.positionOld, 1) : input.position;
#if _ADD_PRECOMPUTED_VELOCITY
    prevPos = prevPos - float4(input.alembicMotionVector, 0);
#endif
    output.previousPositionCSNoJitter = mul(_PrevViewProjMatrix, mul(UNITY_PREV_MATRIX_M, prevPos));
    output.positionWS = TransformObjectToWorld(input.position.xyz);
    return output;
}

float4 Frag(Varyings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    
    half3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
    Ray ray = CreateRay(input.positionWS, -viewDirWS);

    shapeHitCount = TraverseBvh(_NodeBuffer, ray.origin, rcp(ray.dir), hitIds);
    if (shapeHitCount == 0) discard;
    
    InsertionSort(shapeHitIds, shapeHitCount);
    if (!Raymarch(ray, _MotionVectorsMaxSteps, _MotionVectorsMaxDistance, _EpsilonMin, _EpsilonMax)) discard;
    
#if defined(_ALPHATEST_ON)
    Alpha(SampleAlbedoAlpha(input.uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap)).a, _BaseColor, _Cutoff);
#endif

#if defined(APPLICATION_SPACE_WARP_MOTION)
    return float4(CalcAswNdcMotionVectorFromCsPositions(input.positionCSNoJitter, input.previousPositionCSNoJitter), 1);
#else
    return float4(CalcNdcMotionVectorFromCsPositions(input.positionCSNoJitter, input.previousPositionCSNoJitter), 0, 0);
#endif
}

#endif