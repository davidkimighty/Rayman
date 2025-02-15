#ifndef RAYMAN_CEL_LIT_FORWARD
#define RAYMAN_CEL_LIT_FORWARD

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.davidkimighty.rayman/Shaders/Library/Lighting.hlsl"

float _RayShadowBias;
float _MainCelCount;
float _AdditionalCelCount;
float _CelSpread;
float _CelSharpness;
float _SpecularSharpness;
float _RimAmount;
float _RimSmoothness;
float _BlendDiffuse;
float _F0;

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
	inputData.fogCoord = InitializeInputDataFog(float4(input.positionWS, 1.0), input.fogFactorAndVertexLight.x);
	inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
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

inline float CelShading(float value, float celCount)
{
	float celSize = 1.0 / celCount;
	float scaled = saturate(value) / celSize;
	float cel = floor(scaled) * celSize;
	float blend = Sigmoid(frac(scaled), _CelSharpness);
	return lerp(cel, cel + celSize, blend);
}

inline half3 CelLighting(Light light, float celCount, float diffuse, half3 normalWS, half3 viewDirectionWS, float3 F0)
{
	diffuse *= light.distanceAttenuation;
	float cel = CelShading(diffuse, celCount);
	half3 celShade = light.color * cel * lerp(1.0, 0.0, _Metallic);
	
	float roughness = max(1.0 - _Smoothness, EPSILON);
	half specular = GGXSpecular(normalWS, viewDirectionWS, light.direction, F0, roughness);
	specular = Sigmoid(specular, (_Smoothness + _Metallic) * _SpecularSharpness) * light.distanceAttenuation;
	celShade += light.color * specular * cel;
	
	half3 fresnel = GetFresnelSchlick(viewDirectionWS, normalWS, F0) * light.distanceAttenuation;
	float rimIntensity = 1.0 - dot(viewDirectionWS, normalWS);
	half3 rim = smoothstep(_RimAmount - _RimSmoothness, _RimAmount + _RimSmoothness, rimIntensity);
	celShade += light.color * rim * fresnel;

	return celShade * light.shadowAttenuation;
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
#ifdef DYNAMICLIGHTMAP_ON
	output.dynamicLightmapUV = input.dynamicLightmapUV.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
#endif
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
	
	float3 cameraPos = GetCameraPosition();
	half3 rayDir = normalize(input.positionWS - cameraPos);
	Ray ray = CreateRay(input.positionWS, rayDir, _MaxSteps, _MaxDistance);
	ray.distanceTravelled = length(ray.hitPoint - cameraPos);
	
	hitCount = GetHitIds(0, ray, hitIds);
	InsertionSort(hitIds, hitCount.x);
	if (!Raymarch(ray)) discard;
	
	float depth = ray.distanceTravelled - length(input.positionWS - cameraPos) < EPSILON ?
		GetDepth(input.positionWS) : GetDepth(ray.hitPoint);
	
	InputData inputData;
	InitializeInputData(input, ray.hitPoint, normalize(cameraPos - ray.hitPoint), GetNormal(ray.hitPoint), inputData);
	InitializeBakedGIData(input, inputData);
	
	SurfaceData surfaceData;
	InitializeStandardLitSurfaceData(input.uv, surfaceData);
	surfaceData.albedo = baseColor.rgb;
	surfaceData.metallic = _Metallic;
	surfaceData.smoothness = _Smoothness;
	surfaceData.emission = _EmissionColor;
	
	BRDFData brdfData;
	InitializeBRDFData(surfaceData, brdfData);

	BRDFData brdfDataClearCoat = CreateClearCoatBRDFData(surfaceData, brdfData);
	AmbientOcclusionFactor aoFactor = CreateAmbientOcclusionFactor(inputData, surfaceData);
	
	inputData.shadowCoord.z += _RayShadowBias;
	half4 shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);
	Light mainLight = GetMainLight(inputData.shadowCoord, ray.hitPoint, shadowMask);
	
	float3 F0 = lerp(_F0, brdfData.albedo, _Metallic);
	half3 celLighting = 0;
	
#ifdef _LIGHT_LAYERS
	uint meshRenderingLayers = GetMeshRenderingLayer();
	if (IsMatchingLightLayer(mainLight.layerMask, meshRenderingLayers))
#endif
	{
		float nSpread = lerp(_CelSpread, _CelSpread * 0.5, step(1.1, _MainCelCount));
		float celDiffuse = smoothstep(-nSpread, _CelSpread, dot(inputData.normalWS, mainLight.direction));
		celLighting = CelLighting(mainLight, _MainCelCount, celDiffuse, inputData.normalWS, inputData.viewDirectionWS, F0);

		float diffuse = saturate(dot(inputData.normalWS, mainLight.direction)) * mainLight.shadowAttenuation;
		celLighting = lerp(diffuse, celLighting, _BlendDiffuse);
	}
	
#if defined(_ADDITIONAL_LIGHTS)
	uint pixelLightCount = GetAdditionalLightsCount();
#if USE_CLUSTER_LIGHT_LOOP
	[loop] for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
	{
		CLUSTER_LIGHT_LOOP_SUBTRACTIVE_LIGHT_CHECK
		Light light = GetAdditionalLight(lightIndex, ray.hitPoint);
#ifdef _LIGHT_LAYERS
		if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
#endif
		{
			float diffuse = saturate(dot(inputData.normalWS, light.direction));
			celLighting += CelLighting(light, _AdditionalCelCount, diffuse, inputData.normalWS, inputData.viewDirectionWS, F0);
		}
	}
#endif

	LIGHT_LOOP_BEGIN(pixelLightCount)
		Light light = GetAdditionalLight(lightIndex, ray.hitPoint);

#ifdef _LIGHT_LAYERS
		if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
#endif
		{
			float diffuse = saturate(dot(inputData.normalWS, light.direction));
			celLighting += CelLighting(light, _AdditionalCelCount, diffuse, inputData.normalWS, inputData.viewDirectionWS, F0);
		}
	LIGHT_LOOP_END
#endif

#if defined(_ADDITIONAL_LIGHTS_VERTEX)
	celLighting += inputData.vertexLighting * brdfData.diffuse;
#endif

	MixRealtimeAndBakedGI(mainLight, inputData.normalWS, inputData.bakedGI);
	half3 giColor = GlobalIllumination(brdfData, brdfDataClearCoat, surfaceData.clearCoatMask,
		inputData.bakedGI, aoFactor.indirectAmbientOcclusion, inputData.positionWS,
		inputData.normalWS, inputData.viewDirectionWS, inputData.normalizedScreenSpaceUV);
	
	baseColor.rgb *= giColor + celLighting + _EmissionColor;
	baseColor.rgb = MixFog(baseColor.rgb, input.fogFactorAndVertexLight.x);
	
	FragOutput output;
	output.color = baseColor;
	output.depth = depth;
	return output;
}

#endif