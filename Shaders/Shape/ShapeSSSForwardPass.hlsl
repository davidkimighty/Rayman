#ifndef RAYMAN_SHAPE_SSS_FORWARD
#define RAYMAN_SHAPE_SSS_FORWARD

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/SimplexNoise.hlsl"
#include "Packages/com.davidkimighty.rayman/Shaders/Library/Geometry.hlsl"
#include "Packages/com.davidkimighty.rayman/Shaders/Library/Lighting.hlsl"

struct Attributes
{
	float4 positionOS : POSITION;
	float3 normalOS : NORMAL;
	float4 tangentOS : TANGENT;
	float2 texcoord : TEXCOORD0;
	float2 staticLightmapUV : TEXCOORD1;
	float2 dynamicLightmapUV  : TEXCOORD2;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
	float4 positionCS : SV_POSITION;
	float2 uv : TEXCOORD0;
	float3 positionWS : TEXCOORD1;
	float3 normalWS : TEXCOORD2;
	half4 tangentWS : TEXCOORD3;
	
#ifdef _ADDITIONAL_LIGHTS_VERTEX
	half4 fogFactorAndVertexLight : TEXCOORD4; // x: fogFactor, yzw: vertex light
#else
	half  fogFactor : TEXCOORD4;
#endif

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
	float4 shadowCoord : TEXCOORD5;
#endif
	
	DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 6);
#ifdef DYNAMICLIGHTMAP_ON
	float2  dynamicLightmapUV : TEXCOORD7;
#endif
	UNITY_VERTEX_INPUT_INSTANCE_ID
	UNITY_VERTEX_OUTPUT_STEREO
};

struct FragOutput
{
    half4 color : SV_Target;
    float depth : SV_Depth;
};

struct Color
{
	half4 color;
#ifdef _GRADIENT_COLOR
	half4 gradientColor;
	bool useGradient;
#endif
};

CBUFFER_START(Raymarch)
float _EpsilonMin;
float _EpsilonMax;
int _MaxSteps;
float _MaxDistance;
CBUFFER_END

CBUFFER_START(SssParams)
float _SssDistortion;
float _SssPower;
float _SssScale;
float _SssAmbient;
CBUFFER_END

float _GradientScaleY;
float _GradientOffsetY;
float _GradientAngle;
float _RayShadowBias;

half4 baseColor;
#ifdef _SHAPE_GROUP
half4 localColor;
#endif

Texture2D _NoiseTex;
SamplerState sampler_NoiseTex;
uniform half4 _NoiseTex_TexelSize;

StructuredBuffer<Color> _ColorBuffer;

#ifdef _GRADIENT_COLOR
inline half4 GetGradientColor(Color colorData, float3 pos)
{
	float2 uv = (pos.xy - 0.5 + _GradientOffsetY) / (_GradientScaleY / 2.0) + 0.5;
	uv = GetRotatedUV(uv, float2(0.5, 0.5), radians(_GradientAngle));
	uv.y = 1.0 - uv.y;
	uv = saturate(uv);
	return lerp(colorData.color, colorData.gradientColor, uv.y);
}
#endif

inline void InitBlend(const int passType, int index)
{
	if (passType != PASS_MAP) return;
#ifdef _SHAPE_GROUP
	localColor = _ColorBuffer[index].color;
#else
	baseColor = _ColorBuffer[index].color;
#endif
}

inline void PreShapeBlend(const int passType, int index, float3 position, inout float distance)
{
	if (passType == PASS_NORMAL)
	{
		float frequency = 200.0;
		float amplitude = 0.0001;
		distance += snoise(position * frequency) * amplitude; // testing noise
	}
}

inline void PostShapeBlend(const int passType, int index, float3 position, float blend)
{
	if (passType != PASS_MAP) return;
#ifdef _GRADIENT_COLOR
	Color colorData = _ColorBuffer[index];
	half4 color = colorData.useGradient ? GetGradientColor(colorData, position) : colorData.color;
#else
	half4 color = _ColorBuffer[index].color;
#endif
#ifdef _SHAPE_GROUP
	localColor = lerp(localColor, color, blend);
#else
	baseColor = lerp(baseColor, color, blend);
#endif
}

#ifdef _SHAPE_GROUP
inline void PostGroupBlend(const int passType, float blend)
{
	if (passType != PASS_MAP) return;
	baseColor = lerp(baseColor, localColor, blend);
}
#endif

inline void InitializeInputData(Varyings input, float3 positionWS, half3 viewDirectionWS, float3 normalWS, out InputData inputData)
{
	inputData = (InputData)0;
	inputData.positionWS = positionWS;
	inputData.normalWS = normalWS;
	inputData.viewDirectionWS = viewDirectionWS;

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
	inputData.shadowCoord = input.shadowCoord;
#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
	inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
#else
	inputData.shadowCoord = float4(0, 0, 0, 0);
#endif
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
#if defined(_SCREEN_SPACE_IRRADIANCE)
	inputData.bakedGI = SAMPLE_GI(_ScreenSpaceIrradiance, input.positionCS.xy);
#elif defined(DYNAMICLIGHTMAP_ON)
	inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.dynamicLightmapUV, input.vertexSH, inputData.normalWS);
	inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);
#elif !defined(LIGHTMAP_ON) && (defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2))
	inputData.bakedGI = SAMPLE_GI(input.vertexSH,
		GetAbsolutePositionWS(inputData.positionWS),
		inputData.normalWS,
		inputData.viewDirectionWS,
		input.positionCS.xy,
		input.probeOcclusion,
		inputData.shadowMask);
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
	output.normalWS = normalInput.normalWS;
#if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR) || defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR)
	real sign = input.tangentOS.w * GetOddNegativeScale();
	half4 tangentWS = half4(normalInput.tangentWS.xyz, sign);
#endif
#if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR)
	output.tangentWS = tangentWS;
#endif
	
	OUTPUT_LIGHTMAP_UV(input.staticLightmapUV, unity_LightmapST, output.staticLightmapUV);
#ifdef DYNAMICLIGHTMAP_ON
	output.dynamicLightmapUV = input.dynamicLightmapUV.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
#endif
	
	half3 viewDirectionWS = GetWorldSpaceNormalizeViewDir(vertexInput.positionWS);
	OUTPUT_SH4(vertexInput.positionWS, normalInput.normalWS.xyz, viewDirectionWS, output.vertexSH, output.probeOcclusion);

	half fogFactor = 0;
#if !defined(_FOG_FRAGMENT)
	fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
#endif
	half3 vertexLight = VertexLighting(vertexInput.positionWS, normalInput.normalWS);
	
#ifdef _ADDITIONAL_LIGHTS_VERTEX
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
	
    float3 cameraPosWS = _WorldSpaceCameraPos;
    half3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
	
    Ray ray = CreateRay(input.positionWS, -viewDirWS, _EpsilonMin);
	ray.distanceTravelled = length(ray.hitPoint - cameraPosWS);
	
	shapeHitCount = TraverseBvh(_ShapeNodeBuffer, 0, ray.origin, ray.dir, shapeHitIds).x;
	if (shapeHitCount == 0) discard;

	InsertionSort(shapeHitIds, shapeHitCount);
	if (!Raymarch(ray, _MaxSteps, _MaxDistance, float2(_EpsilonMin, _EpsilonMax))) discard;

	float depth = GetDepth(ray.hitPoint);
	float3 normal = GetNormal(ray.hitPoint, ray.epsilon);
    
	InputData inputData;
	InitializeInputData(input, ray.hitPoint, viewDirWS, normal, inputData);
	inputData.shadowCoord.z += _RayShadowBias;
	InitializeBakedGIData(input, inputData);

	float3 posOS = mul(unity_WorldToObject, float4(ray.hitPoint, 1.0)).xyz;
	float2 uv = GetSphereUV(posOS);
	
	SurfaceData surfaceData = (SurfaceData)0;
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
	
	color.rgb = MixFog(color.rgb, input.fogFactor);
	color.a = baseColor.a;
	
	FragOutput output;
	output.color = color;
	output.depth = depth;
	return output;
}

#endif