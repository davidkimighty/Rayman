#ifndef RAYMAN_SHAPE_UNLIT_FORWARD
#define RAYMAN_SHAPE_UNLIT_FORWARD

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

#include "Packages/com.davidkimighty.rayman/Shaders/Library/Geometry.hlsl"
#include "Packages/com.davidkimighty.rayman/Shaders/Library/Lighting.hlsl"

struct Attributes
{
	float4 positionOS : POSITION;
	float2 texcoord : TEXCOORD0;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
	float4 positionCS : SV_POSITION;
	float3 positionWS : TEXCOORD0;
	half  fogFactor : TEXCOORD1;
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
	int useGradient;
#endif
};

CBUFFER_START(Raymarch)
float _EpsilonMin;
float _EpsilonMax;
int _MaxSteps;
float _MaxDistance;
CBUFFER_END

float _OutlineThickness;
float4 _OutlineColor;
float _FresnelPow;
float _GradientScaleY;
float _GradientOffsetY;
float _GradientAngle;

half4 baseColor;
#ifdef _SHAPE_GROUP
half4 localColor;
#endif

StructuredBuffer<Color> _ColorBuffer;

#ifdef _GRADIENT_COLOR
inline half4 GetGradientColor(Color colorData, float3 localPos, float2 halfExtents)
{
	float2 uv = localPos.xy / (halfExtents.xy * 2.0) + 0.5;
	uv.y = (uv.y - 0.5 + _GradientOffsetY) / _GradientScaleY + 0.5;
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
	half4 color = colorData.useGradient ? GetGradientColor(colorData, params.pos, params.size) : colorData.color;
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

Varyings Vert (Attributes input)
{
	Varyings output = (Varyings)0;
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_TRANSFER_INSTANCE_ID(input, output);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
	
	VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
	output.positionCS = vertexInput.positionCS;
	output.positionWS = vertexInput.positionWS;
	half fogFactor = 0;
#if !defined(_FOG_FRAGMENT)
	fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
#endif
	output.fogFactor = fogFactor;
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
	bool isHit = Raymarch(ray, _MaxSteps, _MaxDistance, _EpsilonMin, _EpsilonMax);

	float outlineMax = _EpsilonMin + _OutlineThickness;
	if (ray.minDist > outlineMax) discard;

	float3 depthPos = isHit ? ray.hitPoint : (ray.origin + ray.dir * ray.minDistTravelDist);
	float depth = GetNonLinearDepth(depthPos);

	if (!isHit)
	{
		float delta = fwidth(ray.minDist);
		float outlineAA = saturate(0.5 - (ray.minDist - outlineMax) / delta);
		
		baseColor.rgb = _OutlineColor.rgb;
		//baseColor.a = outlineAA;
	} 
	else
	{
		float3 normal = GetNormal(ray.hitPoint, _EpsilonMin);
		float fresnel = GetFresnel(viewDirWS, normal, _FresnelPow);

		half3 ambient = SampleSH(normal) * fresnel;
		baseColor.rgb += ambient;
		
		half fog = InitializeInputDataFog(float4(input.positionWS, 1.0), input.fogFactor);
		baseColor.rgb = MixFog(baseColor.rgb, fog);
	}
	
	FragOutput output;
	output.color = baseColor;
	output.depth = depth;
	return output;
}

#endif