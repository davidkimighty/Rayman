#ifndef RAYMAN_MENGERSPONGE_SHADOWCASTER
#define RAYMAN_MENGERSPONGE_SHADOWCASTER

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

float3 _LightDirection;
float3 _LightPosition;

struct Attributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float2 texcoord : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float3 positionWS : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

Varyings Vert(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);

    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
    float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

#if _CASTING_PUNCTUAL_LIGHT_SHADOW
    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
#else
    float3 lightDirectionWS = _LightDirection;
#endif

    float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
    positionCS = ApplyShadowClamping(positionCS);
    
    output.positionCS = positionCS;
    output.positionWS = positionWS;
    return output;
}

float Frag(Varyings input) : SV_Depth
{
    UNITY_SETUP_INSTANCE_ID(input);
    
    float3 cameraPosWS = _WorldSpaceCameraPos;
    half3 viewDirWS = -GetWorldSpaceNormalizeViewDir(input.positionWS);
    Ray ray = CreateRay(input.positionWS, viewDirWS);
    ray.distanceTravelled = length(ray.hitPoint - cameraPosWS);
    
    if (!Raymarch(ray, _MaxSteps, _MaxDistance, _EpsilonMin, _EpsilonMax)) discard;
    
    return GetNonLinearDepth(ray.hitPoint);
}

#endif