#ifndef RAYMAN_FORWARD
#define RAYMAN_FORWARD

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
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
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 posCS : SV_POSITION;
    float4 posSS : TEXCOORD0;
    float3 posWS : TEXCOORD1;
    float3 normalWS : TEXCOORD2;
    DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 3);
    half4 fogFactorAndVertexLight : TEXCOORD4;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

struct FragOutput
{
    float4 color : SV_Target;
    float depth : SV_Depth;
};

// Texture2D _MainTex;
// SamplerState sampler_MainTex;
float _ShadowBiasVal;
float _F0;
float _SpecularPow;
float4 _RimColor;
float _RimPow;
float4 _FresnelColor;
float _FresnelPow;

Varyings Vert (Attributes input)
{
    Varyings output = (Varyings)0;
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_TRANSFER_INSTANCE_ID(input, output);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
	
	VertexPositionInputs vertexInput = GetVertexPositionInputs(input.vertex.xyz);
	output.posCS = vertexInput.positionCS;
	output.posWS = vertexInput.positionWS;
	output.normalWS = TransformObjectToWorldNormal(input.normal);
	
	half3 vertexLight = VertexLighting(output.posWS, output.normalWS);
	half fogFactor = ComputeFogFactor(output.posCS.z);
	output.fogFactorAndVertexLight = half4(fogFactor, vertexLight);

	OUTPUT_LIGHTMAP_UV(input.lightmapUV, unity_LightmapST, output.lightmapUV);
	OUTPUT_SH(output.normalWS.xyz, output.vertexSH);
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
	
	TraverseAabbTree(0, ray, hitIds, hitCount);
	InsertionSort(hitIds, hitCount.x);
	
	if (!Raymarch(ray)) discard;

	const float3 normal = GetNormal(ray.hitPoint);
	float lengthToSurface = length(input.posWS - cameraPos);
	const float depth = ray.distanceTravelled - lengthToSurface < EPSILON ?
		GetDepth(input.posWS) : GetDepth(ray.hitPoint);
	
	const float3 viewDir = normalize(cameraPos - ray.hitPoint);
	const float schlick = GetFresnelSchlick(viewDir, normal, _F0);
	
	float3 shade = MainLightShade(ray.hitPoint, ray.dir, _ShadowBiasVal, normal, schlick, _SpecularPow);
	AdditionalLightsShade(ray.hitPoint, ray.dir, normal, schlick, _SpecularPow, shade);
	shade += RimLightShade(normal, viewDir, _RimPow, _RimColor);

	finalColor.rgb *= shade + SAMPLE_GI(input.lightmapUV, input.vertexSH, normal);
	finalColor.rgb = MixFog(finalColor.rgb, input.fogFactorAndVertexLight.x);

	FragOutput output;
	output.color = finalColor;
	output.depth = depth;
	return output;
}

#endif