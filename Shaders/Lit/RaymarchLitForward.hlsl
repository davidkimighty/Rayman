#ifndef RAYMAN_LIT_FORWARD
#define RAYMAN_LIT_FORWARD

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.davidkimighty.rayman/Shaders/Library/Camera.hlsl"
#include "Packages/com.davidkimighty.rayman/Shaders/Library/Geometry.hlsl"

struct Attributes
{
	float4 positionOS : POSITION;
	float3 normalOS : NORMAL;
	float4 tangentOS : TANGENT;
	float2 texcoord : TEXCOORD0;
	float2 staticLightmapUV : TEXCOORD1;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
	float4 positionCS : SV_POSITION;
	float2 uv : TEXCOORD0;
	float3 positionWS : TEXCOORD1;
	float3 normalWS : TEXCOORD2;
	half4 fogFactorAndVertexLight : TEXCOORD3;
	DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 4);
	UNITY_VERTEX_INPUT_INSTANCE_ID
	UNITY_VERTEX_OUTPUT_STEREO
};

struct FragOutput
{
    float4 color : SV_Target;
    float depth : SV_Depth;
};

Varyings Vert (Attributes input)
{
	Varyings output = (Varyings)0;
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_TRANSFER_INSTANCE_ID(input, output);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
	
	VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
	VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

	output.positionCS = vertexInput.positionCS;
	output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
	output.positionWS = vertexInput.positionWS;
	output.normalWS = normalInput.normalWS;
	
	OUTPUT_LIGHTMAP_UV(input.staticLightmapUV, unity_LightmapST, output.staticLightmapUV);
	OUTPUT_SH4(vertexInput.positionWS, output.normalWS.xyz, GetWorldSpaceNormalizeViewDir(vertexInput.positionWS), output.vertexSH, output.probeOcclusion);

	half3 vertexLight = VertexLighting(vertexInput.positionWS, normalInput.normalWS);
	half fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
	output.fogFactorAndVertexLight = half4(fogFactor, vertexLight);
	return output;
}

FragOutput Frag (Varyings input)
{
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
	
	const float3 cameraPos = GetCameraPosition();
	const float3 rayDir = normalize(input.positionWS - cameraPos);
	Ray ray = CreateRay(input.positionWS, rayDir, _MaxSteps, _MaxDistance);
	ray.distanceTravelled = length(ray.hitPoint - cameraPos);
	
	hitCount = GetHitIds(0, ray, hitIds);
	InsertionSort(hitIds, hitCount.x);
	
	if (!Raymarch(ray)) discard;
	
	const float depth = ray.distanceTravelled - length(input.positionWS - cameraPos) < EPSILON ?
		GetDepth(input.positionWS) : GetDepth(ray.hitPoint);

	SurfaceData surfaceData;
	InitializeStandardLitSurfaceData(input.uv, surfaceData);

	half4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
	surfaceData.albedo = baseColor.rgb * baseMap.rgb;
	surfaceData.metallic = _Metallic;
	surfaceData.smoothness = _Smoothness;

	InputData inputData = (InputData)0;
	inputData.positionWS = input.positionWS;
	inputData.normalWS = GetNormal(ray.hitPoint);
	inputData.viewDirectionWS = normalize(cameraPos - ray.hitPoint);
	inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
	inputData.fogCoord = ComputeFogFactor(input.positionCS.z);

	inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.vertexSH, inputData.normalWS);
	inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);

	half4 color = UniversalFragmentPBR(inputData, surfaceData);
	color += _EmissionColor;
	
	FragOutput output;
	output.color = color;
	output.depth = depth;
	return output;
}

#endif