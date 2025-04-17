#ifndef RAYMAN_LIT_FORWARD
#define RAYMAN_LIT_FORWARD

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.davidkimighty.rayman/Shaders/Shared/Debug.hlsl"

float _RayShadowBias;

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
	half4 fogFactorAndVertexLight : TEXCOORD2;
	DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 3);
	UNITY_VERTEX_INPUT_INSTANCE_ID
	UNITY_VERTEX_OUTPUT_STEREO
};

struct FragOutput
{
    half4 color : SV_Target;
    float depth : SV_Depth;
};

inline void InitializeInputData(Varyings input, float3 positionWS, half3 viewDirectionWS, float3 normalWS, out InputData inputData)
{
	inputData = (InputData)0;
	inputData.positionWS = positionWS;
	inputData.normalWS = normalWS;
	inputData.viewDirectionWS = viewDirectionWS;
	inputData.shadowCoord = TransformWorldToShadowCoord(positionWS);
	inputData.fogCoord = InitializeInputDataFog(float4(input.positionWS, 1.0), input.fogFactorAndVertexLight.x);
	inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
	inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
}

inline void InitializeBakedGIData(Varyings input, inout InputData inputData)
{
	inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.vertexSH, inputData.normalWS);
	inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);
}

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
	half3 viewDirectionWS = GetWorldSpaceNormalizeViewDir(vertexInput.positionWS);
	
	OUTPUT_LIGHTMAP_UV(input.staticLightmapUV, unity_LightmapST, output.staticLightmapUV);
	OUTPUT_SH4(vertexInput.positionWS, normalInput.normalWS.xyz, viewDirectionWS, output.vertexSH, output.probeOcclusion);

	half fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
	half3 vertexLight = VertexLighting(vertexInput.positionWS, normalInput.normalWS);
	output.fogFactorAndVertexLight = half4(fogFactor, vertexLight);
	return output;
}

FragOutput Frag (Varyings input)
{
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

	const float3 cameraPos = GetCameraPosition();
	const float3 rayDir = normalize(input.positionWS - cameraPos);
	Ray ray = CreateRay(input.positionWS, rayDir, _EpsilonMin);
	ray.distanceTravelled = length(ray.hitPoint - cameraPos);
	
	hitCount = TraverseBvh(0, ray.origin, ray.dir, hitIds);
	if (hitCount.x == 0) discard;
	
	InsertionSort(hitIds, hitCount.x);
#ifdef DEBUG_MODE
	FragOutput debugOutput;
	debugOutput.color = DebugRaymarch(input.positionWS, cameraPos,
		ray, _MaxSteps, _MaxDistance, float2(_EpsilonMin, _EpsilonMax), debugOutput.depth);
	return debugOutput;
#endif
	if (!Raymarch(ray, _MaxSteps, _MaxDistance, float2(_EpsilonMin, _EpsilonMax))) discard;

	float3 viewDir = normalize(cameraPos - ray.hitPoint);
	float3 normal = GetNormal(ray.hitPoint, ray.epsilon);
	float depth = ray.distanceTravelled - length(input.positionWS - cameraPos) < ray.epsilon ?
		GetDepth(input.positionWS) : GetDepth(ray.hitPoint);
	
	InputData inputData;
	InitializeInputData(input, ray.hitPoint, viewDir, normal, inputData);
	inputData.shadowCoord.z += _RayShadowBias;
	InitializeBakedGIData(input, inputData);
	
	SurfaceData surfaceData = (SurfaceData)0;
	InitializeStandardLitSurfaceData(input.uv, surfaceData);
	surfaceData.albedo = baseColor.rgb * _BaseMap.Sample(sampler_BaseMap, input.uv);
	surfaceData.metallic = _Metallic;
	surfaceData.smoothness = _Smoothness;
	surfaceData.emission = _EmissionColor;

	half4 color = UniversalFragmentPBR(inputData, surfaceData);
	
	color.rgb = MixFog(color.rgb, input.fogFactorAndVertexLight.x);
	color.a = baseColor.a;

	FragOutput output;
	output.color = color;
	output.depth = depth;
	return output;
}

#endif