#ifndef RAYMAN_RETRO_LIT_FORWARD
#define RAYMAN_RETRO_LIT_FORWARD

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.davidkimighty.rayman/Shaders/Library/Lighting.hlsl"

float _RayShadowBias;

CBUFFER_START(CelParams)
float _CelCount;
float _CelSpread;
float _CelSharpness;
float _F0;
CBUFFER_END

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

inline float CelShading(float value, float celCount)
{
	float celSize = 1.0 / celCount;
	float scaled = saturate(value) / celSize;
	float cel = floor(scaled) * celSize;
	float blend = Sigmoid(frac(scaled), _CelSharpness);
	return lerp(cel, cel + celSize, blend);
}

inline half3 CelLighting(BRDFData brdfData, Light light, half3 normalWS, half3 viewDirectionWS, float3 F0)
{
	float lightAttenuation = light.distanceAttenuation * light.shadowAttenuation;
	
	half NdotL = dot(normalWS, light.direction);
	half spread = lerp(_CelSpread, _CelSpread * 0.5, step(1.1, _CelCount));
	half celDiffuse = CelShading(smoothstep(-spread, _CelSpread, NdotL), _CelCount);
	half3 radiance = light.color * (saturate(lightAttenuation) * celDiffuse);
	
	half specular = DirectBRDFSpecular(brdfData, normalWS, light.direction, viewDirectionWS);
	radiance += light.color * saturate(lightAttenuation) * celDiffuse * specular ;

	// half roughness = saturate(1.0 - brdfData.roughness2);
	// half3 fresnel = GetFresnelSchlick(viewDirectionWS, normalWS, F0);
	// radiance += light.color * lightAttenuation * fresnel * roughness;

	return brdfData.diffuse * radiance;
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
	if (!Raymarch(ray, _MaxSteps, _MaxDistance, float2(_EpsilonMin, _EpsilonMax))) discard;

	float3 viewDir = normalize(cameraPos - ray.hitPoint);
	float3 normal = GetNormal(ray.hitPoint, ray.epsilon);
	float depth = ray.distanceTravelled - length(input.positionWS - cameraPos) < ray.epsilon ?
		GetDepth(input.positionWS) : GetDepth(ray.hitPoint);
	
	InputData inputData;
	InitializeInputData(input, ray.hitPoint, viewDir, normal, inputData);
	inputData.shadowCoord.z += _RayShadowBias;
	
	SurfaceData surfaceData = (SurfaceData)0;
	InitializeStandardLitSurfaceData(input.uv, surfaceData);
	surfaceData.albedo = baseColor.rgb * _BaseMap.Sample(sampler_BaseMap, input.uv);
	surfaceData.metallic = _Metallic;
	surfaceData.smoothness = _Smoothness;
	surfaceData.emission = _EmissionColor;

	BRDFData brdfData;
	InitializeBRDFData(surfaceData, brdfData);

	AmbientOcclusionFactor aoFactor = CreateAmbientOcclusionFactor(inputData, surfaceData);
	half4 shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);
	Light mainLight = GetMainLight(inputData, shadowMask, aoFactor);
	
	float3 F0 = lerp(_F0, brdfData.albedo, _Metallic);
	half3 celLighting = 0;
	
#ifdef _LIGHT_LAYERS
	uint meshRenderingLayers = GetMeshRenderingLayer();
	if (IsMatchingLightLayer(mainLight.layerMask, meshRenderingLayers))
		#endif
	{
		celLighting = CelLighting(brdfData, mainLight, inputData.normalWS, inputData.viewDirectionWS, F0);
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
			celLighting += CelLighting(brdfData, light, inputData.normalWS, inputData.viewDirectionWS, F0);
		}
	}
#endif

	LIGHT_LOOP_BEGIN(pixelLightCount)
		Light light = GetAdditionalLight(lightIndex, ray.hitPoint);

#ifdef _LIGHT_LAYERS
	if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
#endif
	{
		celLighting += CelLighting(brdfData, light, inputData.normalWS, inputData.viewDirectionWS, F0);
	}
	LIGHT_LOOP_END
#endif

	half4 color = half4(celLighting, baseColor.a);
	color.rgb = MixFog(color.rgb, input.fogFactorAndVertexLight.x);
	
	FragOutput output;
	output.color = color;
	output.depth = depth;
	return output;
}

#endif