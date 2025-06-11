#ifndef RAYMAN_SSS_LIT_FORWARD
#define RAYMAN_SSS_LIT_FORWARD

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.davidkimighty.rayman/Shaders/Library/Lighting.hlsl"

float _RayShadowBias;

CBUFFER_START(SssParams)
float _SssDistortion;
float _SssPower;
float _SssScale;
float _SssAmbient;
CBUFFER_END

Texture2D _NoiseTex; // Declare the texture
SamplerState sampler_NoiseTex;
uniform half4 _NoiseTex_TexelSize;

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

float hash( float n )
{
	return frac(sin(n)*43758.5453);
}

float noise( float3 x )
{
	float3 p = floor(x);
	float3 f = frac(x);
	f = f*f*(3.0-2.0*f);

	// Create a pseudo-random UV based on the integer part of the position
	float n = p.x + p.y*57.0 + 113.0*p.z;
	float2 uv = float2(hash(n), hash(n+1.0)); // Sample two different "slices"

	// Sample the texture
	float2 rg = _NoiseTex.SampleLevel(sampler_NoiseTex, (uv + 0.5) / _NoiseTex_TexelSize.zw, 0.0).xy;

	return lerp( rg.x, rg.y, f.z );
}

float fbm4( float3 p )
{
	float n = 0.0;
	n += 1.000 * noise( p * 1.0 );
	n += 0.500 * noise( p * 2.0 );
	n += 0.250 * noise( p * 4.0 );
	n += 0.125 * noise( p * 8.0 );
	return n;
}

float calculateNoiseGradient(float3 p, float frequency)
{
	float2 epsilon = float2(0.001, 0.0);
	return float3(
		fbm4((p + epsilon.xyy) * frequency) - fbm4((p - epsilon.xyy) * frequency),
		fbm4((p + epsilon.yxy) * frequency) - fbm4((p - epsilon.yxy) * frequency),
		fbm4((p + epsilon.yyx) * frequency) - fbm4((p - epsilon.yyx) * frequency)
	) / (2.0 * epsilon.x * frequency);
}

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
	
    float3 cameraPos = _WorldSpaceCameraPos;
    half3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
    Ray ray = CreateRay(input.positionWS, -viewDirWS, _EpsilonMin);
	ray.distanceTravelled = length(ray.hitPoint - cameraPos);
	
	hitCount = TraverseBvh(0, ray.origin, ray.dir, hitIds);
	if (hitCount.x == 0) discard;
	
	InsertionSort(hitIds, hitCount.x);
	if (!Raymarch(ray, _MaxSteps, _MaxDistance, float2(_EpsilonMin, _EpsilonMax))) discard;
	
	float3 normal = GetNormal(ray.hitPoint, ray.epsilon);
	float depth = ray.distanceTravelled - length(input.positionWS - cameraPos) < ray.epsilon ?
			GetDepth(input.positionWS) : GetDepth(ray.hitPoint);

	InputData inputData;
    InitializeInputData(input, ray.hitPoint, viewDirWS, normal, inputData);
	inputData.shadowCoord.z += _RayShadowBias;
	InitializeBakedGIData(input, inputData);

	float3 posOS = mul(unity_WorldToObject, float4(ray.hitPoint, 1.0)).xyz;
	float2 uv = GetSphereUV(posOS);
	
	SurfaceData surfaceData;
	InitializeStandardLitSurfaceData(uv, surfaceData);
	surfaceData.albedo = baseColor.rgb * _BaseMap.Sample(sampler_BaseMap, uv);
	surfaceData.metallic = _Metallic;
	surfaceData.smoothness = _Smoothness;
	surfaceData.emission = _EmissionColor;
	
	half4 color = UniversalFragmentPBR(inputData, surfaceData);

	Light mainLight = GetMainLight();
	const float invNormalAO = GetAmbientOcclusion(ray.hitPoint, -inputData.normalWS, 3);

#ifdef _LIGHT_LAYERS
	uint meshRenderingLayers = GetMeshRenderingLayer();
	if (IsMatchingLightLayer(mainLight.layerMask, meshRenderingLayers))
#endif
	{
		color.rgb += mainLight.color * SSSApproximation(mainLight, inputData.viewDirectionWS, inputData.normalWS,
			_SssDistortion, _SssPower, _SssScale, _SssAmbient, 1.0 - invNormalAO);
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
			color.rgb += light.color * SSSApproximation(light, inputData.viewDirectionWS, inputData.normalWS,
				_SssDistortion, _SssPower, _SssScale, _SssAmbient, 1.0 - invNormalAO);
		}
	}
#endif

	LIGHT_LOOP_BEGIN(pixelLightCount)
		Light light = GetAdditionalLight(lightIndex, ray.hitPoint);

#ifdef _LIGHT_LAYERS
		if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
#endif
		{
			color.rgb += light.color * SSSApproximation(light, inputData.viewDirectionWS, inputData.normalWS,
				_SssDistortion, _SssPower, _SssScale, _SssAmbient, 1.0 - invNormalAO);
		}
	LIGHT_LOOP_END
#endif
	
	color.rgb = MixFog(color.rgb, input.fogFactorAndVertexLight.x);
	color.a = baseColor.a;

	FragOutput output;
	output.color = color;
	output.depth = depth;
	return output;
}

#endif