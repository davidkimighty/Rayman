#ifndef RAYMAN_LIT_FORWARD
#define RAYMAN_LIT_FORWARD

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

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
#ifdef _ADDITIONAL_LIGHTS_VERTEX
	half4 fogFactorAndVertexLight : TEXCOORD2;
#else
	half  fogFactor : TEXCOORD2;
#endif
	DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 3);
#ifdef DYNAMICLIGHTMAP_ON
	float2  dynamicLightmapUV : TEXCOORD4;
#endif
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
#ifdef _ADDITIONAL_LIGHTS_VERTEX
	inputData.fogCoord = InitializeInputDataFog(float4(input.positionWS, 1.0), input.fogFactorAndVertexLight.x);
	inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
#else
	inputData.fogCoord = InitializeInputDataFog(float4(input.positionWS, 1.0), input.fogFactor);
#endif
	inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
}

inline void InitializeBakedGIData(Varyings input, inout InputData inputData)
{
#if defined(DYNAMICLIGHTMAP_ON)
	inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.dynamicLightmapUV, input.vertexSH, inputData.normalWS);
	inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);
#elif !defined(LIGHTMAP_ON) && (defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2))
	inputData.bakedGI = SAMPLE_GI(input.vertexSH, GetAbsolutePositionWS(inputData.positionWS),
		inputData.normalWS, inputData.viewDirectionWS, input.positionCS.xy, input.probeOcclusion, inputData.shadowMask);
#else
	inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.vertexSH, inputData.normalWS);
	inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);
#endif
}

#if defined(DEBUG_MODE)
#define B float3(0.0, 0.3, 1.0)
#define Y float3(1.0, 0.8, 0.0)

int _DebugMode;
int _BoundsDisplayThreshold;

inline void Debugging(Ray ray, float3 posWS, float3 cameraPos, out FragOutput output)
{
	int raymarchCount;
	bool rayHit = RaymarchHitCount(ray, raymarchCount);
	output.depth = 1;
	
	switch (_DebugMode)
	{
		case 2:
			if (!rayHit) discard;
			output.color = float4(GetNormal(ray.hitPoint) * 0.5 + 0.5, 1);
			output.depth = ray.distanceTravelled - length(posWS - cameraPos) < EPSILON ?
				GetDepth(posWS) : GetDepth(ray.hitPoint);
			break;
		case 3:
			output.color = float4(GetHitMap(raymarchCount, ray.maxSteps, B, Y), 1);
			break;
		case 4:
			int total = hitCount.x + hitCount.y;
			output.color = 1 * saturate((float)total / (total + _BoundsDisplayThreshold));
			break;
	}
}
#endif

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
#ifdef DYNAMICLIGHTMAP_ON
	output.dynamicLightmapUV = input.dynamicLightmapUV.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
#endif
	OUTPUT_SH4(vertexInput.positionWS, normalInput.normalWS.xyz, viewDirectionWS, output.vertexSH, output.probeOcclusion);
	
	half fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
#ifdef _ADDITIONAL_LIGHTS_VERTEX
	half3 vertexLight = VertexLighting(vertexInput.positionWS, normalInput.normalWS);
	output.fogFactorAndVertexLight = half4(fogFactor, vertexLight);
#else
	output.fogFactor = fogFactor;
#endif
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
//
// #if defined(DEBUG_MODE)
// 	FragOutput output;
// 	Debugging(ray, input.positionWS, cameraPos, output);
// 	return output;
// #endif
// 	
	if (!Raymarch(ray)) discard;
	
	const float depth = ray.distanceTravelled - length(input.positionWS - cameraPos) < EPSILON ?
		GetDepth(input.positionWS) : GetDepth(ray.hitPoint);

	InputData inputData;
	InitializeInputData(input, ray.hitPoint, normalize(cameraPos - ray.hitPoint), GetNormal(ray.hitPoint), inputData);
	inputData.shadowCoord.z += _RayShadowBias;
	InitializeBakedGIData(input, inputData);
	
	SurfaceData surfaceData;
	InitializeStandardLitSurfaceData(input.uv, surfaceData);
	surfaceData.albedo = baseColor.rgb;
	surfaceData.metallic = _Metallic;
	surfaceData.smoothness = _Smoothness;

	half4 color = UniversalFragmentPBR(inputData, surfaceData);
	color.rgb = MixFog(color.rgb, input.fogFactor);

	FragOutput output;
	output.color = color;
	output.depth = depth;
	return output;
}

#endif