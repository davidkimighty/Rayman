#ifndef RAYMAN_SHAPE_CEL_FORWARD
#define RAYMAN_SHAPE_CEL_FORWARD

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

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
    half4 color : SV_Target0;
    float depth : SV_Depth;
#ifdef _WRITE_RENDERING_LAYERS
	uint outRenderingLayers : SV_Target1
#endif
};

struct Color
{
	half4 color;
#ifdef _GRADIENT_COLOR
	half4 gradientColor;
	bool useGradient;
#endif
};

TEXTURE2D(_CelTex);
SAMPLER(sampler_CelTex);

CBUFFER_START(MatParams)
float _EpsilonMin;
float _EpsilonMax;
int _MaxSteps;
float _MaxDistance;

float4 _CelTex_ST;
float _CelTexScale;
float _CelTexRange;
float _CelCount;
float _CelSpread;
float _CelSmooth;
float _BlendDiffuse;

float _SpecIntensity;
float _SpecTexRange;
float _SpecCelSpread;
float _SpecSmooth;

float _RimIntensity;
float _RimTexRange;
float _RimCelSpread;
float _RimSmooth;
float _F0;

float _GradientScaleY;
float _GradientOffsetY;
float _GradientAngle;
float _RayShadowBias;

half4 baseColor;
#ifdef _SHAPE_GROUP
half4 localColor;
#endif
CBUFFER_END

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

inline void PreShapeBlend(const int passType, BlendParams params, inout float shapeDistance) { }

inline void PostShapeBlend(const int passType, BlendParams params, inout float combinedDistance)
{
	if (passType != PASS_MAP) return;
#ifdef _GRADIENT_COLOR
	Color colorData = _ColorBuffer[params.index];
	half4 color = colorData.useGradient ? GetGradientColor(colorData, params.pos) : colorData.color;
#else
	half4 color = _ColorBuffer[params.index].color;
#endif
#ifdef _SHAPE_GROUP
	localColor = lerp(localColor, color, params.blend);
#else
	baseColor = lerp(baseColor, color, params.blend);
#endif
}

#ifdef _SHAPE_GROUP
inline void PostGroupBlend(const int passType, float blend)
{
	if (passType != PASS_MAP) return;
	baseColor = lerp(baseColor, localColor, blend);
}
#endif

inline float CelShade(float lightVal, float mask, float spread, float count, float softness)
{
	float scaled = (lightVal + (mask - 1.0)) / spread + 1.0;
	float smoothFrac = smoothstep(1.0 - softness, 1.0, frac(scaled));
	float cel = floor(scaled) + smoothFrac;
	cel /= count;
	return saturate(cel);
}

inline half3 CelLighting(BRDFData brdfData, Light light, float2 uv, half3 normalWS, half3 viewDirectionWS, float3 F0)
{
	half lightAttenuation = light.distanceAttenuation * light.shadowAttenuation;
	half NdotL = dot(normalWS, light.direction);
	half mask = SAMPLE_TEXTURE2D(_CelTex, sampler_CelTex, uv * _CelTexScale).r;

	half celMask = lerp(1.0 - _CelTexRange, 1.0, mask);
	half celDiffuse = CelShade(NdotL, celMask, _CelSpread, _CelCount, _CelSmooth);
	celDiffuse = lerp(saturate(NdotL), celDiffuse, _BlendDiffuse);
	half3 radiance = light.color * lightAttenuation * celDiffuse;

	half specular = DirectBRDFSpecular(brdfData, normalWS, light.direction, viewDirectionWS);
	specular = Sigmoid(specular, _Smoothness + _Metallic);
	half specCelMask = lerp(0, _SpecTexRange, 1.0 - mask);
	specular = CelShade(specular, specCelMask, _SpecCelSpread, 1.0, _SpecSmooth) * _SpecIntensity;
	radiance += light.color * lightAttenuation * (celDiffuse * specular);
	
	half roughness = saturate(1.0 - brdfData.roughness);
	half rimIntensity = 1.0 - dot(viewDirectionWS, normalWS);
	half rimCelMask = lerp(0, _RimTexRange, mask);
	half rim = CelShade(rimIntensity, rimCelMask, _RimCelSpread, 1.0, _RimSmooth) * _RimIntensity;
	radiance += light.color * lightAttenuation * roughness * rim;
	return brdfData.diffuse * radiance;
}

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
	
    half3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
    Ray ray = CreateRay(input.positionWS, -viewDirWS);

	hitCount = TraverseBvh(_NodeBuffer, 0, ray.origin, ray.dir, hitIds);
	if (hitCount == 0) discard;

	InsertionSort(hitIds, hitCount);
	if (!Raymarch(ray, _MaxSteps, _MaxDistance, _EpsilonMin, _EpsilonMax)) discard;

	float depth = GetNonLinearDepth(ray.hitPoint);
	float3 normal = GetNormal(ray.hitPoint, _EpsilonMin);
    
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

	BRDFData brdfData;
	InitializeBRDFData(surfaceData, brdfData);

	BRDFData brdfDataClearCoat = CreateClearCoatBRDFData(surfaceData, brdfData);
	AmbientOcclusionFactor aoFactor = CreateAmbientOcclusionFactor(inputData, surfaceData);
	
	half4 shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);
	Light mainLight = GetMainLight(inputData, shadowMask, aoFactor);
	
	MixRealtimeAndBakedGI(mainLight, inputData.normalWS, inputData.bakedGI);
	
	float3 F0 = lerp(_F0, brdfData.albedo, _Metallic);
	half3 celLighting = 0;
	
#ifdef _LIGHT_LAYERS
	uint meshRenderingLayers = GetMeshRenderingLayer();
	if (IsMatchingLightLayer(mainLight.layerMask, meshRenderingLayers))
#endif
	{
		celLighting = CelLighting(brdfData, mainLight, uv, inputData.normalWS, inputData.viewDirectionWS, F0);
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
			celLighting += CelLighting(brdfData, light, uv, inputData.normalWS, inputData.viewDirectionWS, F0);
		}
	}
#endif

	LIGHT_LOOP_BEGIN(pixelLightCount)
		Light light = GetAdditionalLight(lightIndex, ray.hitPoint);

#ifdef _LIGHT_LAYERS
		if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
#endif
		{
			celLighting += CelLighting(brdfData, light, uv, inputData.normalWS, inputData.viewDirectionWS, F0);
		}
	LIGHT_LOOP_END
#endif

#if defined(_ADDITIONAL_LIGHTS_VERTEX)
	celLighting += inputData.vertexLighting;
#endif
	
	half3 giColor = GlobalIllumination(brdfData, brdfDataClearCoat, surfaceData.clearCoatMask,
		inputData.bakedGI, aoFactor.indirectAmbientOcclusion, inputData.positionWS,
		inputData.normalWS, inputData.viewDirectionWS, inputData.normalizedScreenSpaceUV);

	half4 color = half4(giColor + celLighting + _EmissionColor, baseColor.a);
	color.rgb = MixFog(color.rgb, input.fogFactor);
	
	FragOutput output;
	output.color = color;
	output.depth = depth;
#ifdef _WRITE_RENDERING_LAYERS
	output.outRenderingLayers = EncodeMeshRenderingLayer();
#endif
	return output;
}

#endif