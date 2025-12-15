#ifndef RAYMAN_SHAPE_MATCAP_FORWARD
#define RAYMAN_SHAPE_MATCAP_FORWARD

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

#include "Packages/com.davidkimighty.rayman/Shaders/Library/Geometry.hlsl"
#include "Packages/com.davidkimighty.rayman/Shaders/Library/Lighting.hlsl"

struct Attributes
{
	float4 positionOS : POSITION;
	float2 texcoord : TEXCOORD0;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
	float4 positionCS : SV_POSITION;
	float3 positionWS : TEXCOORD0;
	UNITY_VERTEX_INPUT_INSTANCE_ID
	UNITY_VERTEX_OUTPUT_STEREO
};

struct FragOutput
{
    half4 color : SV_Target;
    float depth : SV_Depth;
};

CBUFFER_START(Raymarch)
float _EpsilonMin;
float _EpsilonMax;
int _MaxSteps;
float _MaxDistance;
CBUFFER_END

float4 _FresnelColor;
float _FresnelPow;

Varyings Vert (Attributes input)
{
	Varyings output = (Varyings)0;
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_TRANSFER_INSTANCE_ID(input, output);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
	
	VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
	output.positionCS = vertexInput.positionCS;
	output.positionWS = vertexInput.positionWS;
	return output;
}

FragOutput Frag (Varyings input)
{
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
	
    float3 cameraPos = _WorldSpaceCameraPos;
    half3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
	
    Ray ray = CreateRay(input.positionWS, -viewDirWS, _EpsilonMin);
	ray.distanceTravelled = length(ray.hitPoint - cameraPos);
	
	shapeHitCount = TraverseBvh(_ShapeNodeBuffer, 0, ray.origin, ray.dir, shapeHitIds).x;
	if (shapeHitCount == 0) discard;

	InsertionSort(shapeHitIds, shapeHitCount);
	if (!Raymarch(ray, _MaxSteps, _MaxDistance, float2(_EpsilonMin, _EpsilonMax))) discard;

	float depth = ray.distanceTravelled - length(input.positionWS - cameraPos) < ray.epsilon ?
		GetDepth(input.positionWS) : GetDepth(ray.hitPoint);
	float3 normal = GetNormal(ray.hitPoint, ray.epsilon);

	const float2 uv = GetMatCap(viewDirWS, normal);
	float4 finalColor = _BaseMap.Sample(sampler_BaseMap, uv);

	const float fresnel = GetFresnel(viewDirWS, normal, _FresnelPow);
	finalColor.rgb = lerp(finalColor.rgb, _FresnelColor, fresnel);
	
	FragOutput output;
	output.color = finalColor;
	output.depth = depth;
	return output;
}

#endif