#ifndef RAYMAN_SHAPE_DEBUG_FORWARD
#define RAYMAN_SHAPE_DEBUG_FORWARD

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
#include "Packages/com.davidkimighty.rayman/Shaders/Library/Geometry.hlsl"

#define B float3(0.0, 0.3, 1.0)
#define Y float3(1.0, 0.8, 0.0)

struct Attributes
{
	float4 positionOS : POSITION;
	float3 normalOS : NORMAL;
	float4 tangentOS : TANGENT;
	float2 texcoord : TEXCOORD0;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
	float4 positionCS : SV_POSITION;
	float2 uv : TEXCOORD0;
	float3 positionWS : TEXCOORD1;
	float3 normalWS : TEXCOORD2;
	half4 tangentWS : TEXCOORD3;
	UNITY_VERTEX_INPUT_INSTANCE_ID
	UNITY_VERTEX_OUTPUT_STEREO
};

struct FragOutput
{
    half4 color : SV_Target;
    float depth : SV_Depth;
};

CBUFFER_START(Raymarch)
float _EpsilonMin;
float _EpsilonMax;
int _MaxSteps;
float _MaxDistance;
CBUFFER_END

int _DebugMode;
int _BoundsDisplayThreshold;

inline float3 GetHitMap(const int hit, const int maxSteps, const float3 col1, const float3 col2)
{
	float n = clamp(float(hit) / float(maxSteps), 0.0, 1.0);
	return float3(lerp(col1, col2, smoothstep(0.0, 1.0, n)));
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

	int2 bvhCount = TraverseBvh(_ShapeNodeBuffer, 0, ray.origin, ray.dir, shapeHitIds);
	shapeHitCount = bvhCount.x;
	InsertionSort(shapeHitIds, shapeHitCount);
	
	int rayHitCount;
	bool rayHit = RaymarchHitCount(ray, _MaxSteps, _MaxDistance, float2(_EpsilonMin, _EpsilonMax), rayHitCount);

	half4 color = half4(0, 0, 0, 1);
	float depth = GetDepth(lerp(input.positionWS, ray.hitPoint, (float)rayHit));

	[branch]
	if (_DebugMode == 0)
	{
		if (!rayHit) discard;

		float3 normal = GetNormal(ray.hitPoint, ray.epsilon);
		half3 normalColor = floor((normal * 0.5 + 0.5) * 2.0) / 2.0;
		normalColor = pow(0.5 + 0.5 * normal, 2.2);
		normalColor = pow(normal * 0.5 + 0.5, 0.8);
		color = half4(normalColor, 1);
	}
	else if (_DebugMode == 1)
	{
		color = half4(GetHitMap(rayHitCount, _MaxSteps, B, Y), 1);
	}
	else if (_DebugMode == 2)
	{
		int total = bvhCount.x + bvhCount.y;
		color = 1 * saturate((float)total / (total + _BoundsDisplayThreshold));
	}
	
	FragOutput output;
	output.color = color;
	output.depth = depth;
	return output;
}

#endif