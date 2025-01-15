#ifndef RAYMAN_TEXTURE_LIT_FORWARD
#define RAYMAN_TEXTURE_LIT_FORWARD

#include "Packages/com.davidkimighty.rayman/Shaders/Library/Camera.hlsl"
#include "Packages/com.davidkimighty.rayman/Shaders/Library/Geometry.hlsl"
#include "Packages/com.davidkimighty.rayman/Shaders/Library/Lighting.hlsl"

struct Attributes
{
    float4 vertex : POSITION;
    float3 normal : NORMAL;
    float4 tangent : TANGENT;
    float2 texcoord : TEXCOORD0;
    float2 lightmapUV : TEXCOORD1;
};

struct Varyings
{
    float4 posCS : SV_POSITION;
    float4 posSS : TEXCOORD0;
    float3 posWS : TEXCOORD1;
    float3 normalWS : TEXCOORD2;
};

struct FragOutput
{
    float4 color : SV_Target;
    float depth : SV_Depth;
};

Texture2D _MainTex;
SamplerState sampler_MainTex;
float4 _FresnelColor;
float _FresnelPow;

Varyings Vert (Attributes input)
{
    Varyings output = (Varyings)0;
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_TRANSFER_INSTANCE_ID(input, output);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
	
	output.posCS = TransformObjectToHClip(input.vertex.xyz);
	output.posWS = TransformObjectToWorld(input.vertex.xyz);
    return output;
}

FragOutput Frag (Varyings input)
{
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

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
	
	const float3 viewDir = normalize(cameraPos - ray.hitPoint);
	const float2 uv = GetMatCap(viewDir, normal);
	float4 finalColor = _MainTex.Sample(sampler_MainTex, uv);
	
	const float fresnel = GetFresnel(viewDir, normal, _FresnelPow);
	finalColor.rgb = lerp(finalColor.rgb, _FresnelColor, fresnel);

	FragOutput output;
	output.color = finalColor;
	output.depth = depth;
	return output;
}

#endif