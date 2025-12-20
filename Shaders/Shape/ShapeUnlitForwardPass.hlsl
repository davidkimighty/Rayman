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

float _GradientScaleY;
float _GradientOffsetY;
float _GradientAngle;
float4 _FresnelColor;
float _FresnelPow;

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


float DetectEdge(float3 worldPos, float3 viewDirWS)
{
	float offset = 0.01;

	half3 normViewDir = normalize(viewDirWS);
	half3 viewDirTowardsScene = -normViewDir;

	half3 arbitrary = abs(normViewDir.y) > 0.99 ? half3(1, 0, 0) : half3(0, 1, 0);
	half3 viewRight = normalize(cross(viewDirTowardsScene, arbitrary));
	half3 viewUp = normalize(cross(viewRight, viewDirTowardsScene));

	float c = NormalMap(worldPos);
	float dR = NormalMap(worldPos + viewRight * offset);
	float dL = NormalMap(worldPos - viewRight * offset);
	float dU = NormalMap(worldPos + viewUp * offset);
	float dD = NormalMap(worldPos - viewUp * offset);
	float sum = dR + dL + dU + dD; 
	return dU;
}

float DetectEdge2(float3 worldPos, half3 viewDirWS)
{
    half3 normViewDir = normalize(viewDirWS);
    half3 viewDirTowardsScene = -normViewDir;

    half3 arbitrary = abs(normViewDir.y) > 0.99 ? half3(1, 0, 0) : half3(0, 1, 0);
    half3 viewRight = normalize(cross(viewDirTowardsScene, arbitrary));
    half3 viewUp = normalize(cross(viewRight, viewDirTowardsScene));

	float scale = 0.5;
    float3 cameraPosWS = _WorldSpaceCameraPos;
    float dist = length(worldPos - cameraPosWS);
    float fovY = 2.0 * atan(1.0 / UNITY_MATRIX_P[1][1]);
    float anglePerPixel = fovY / _ScreenParams.y;
    float pixelWorldSize = 2.0 * dist * tan(anglePerPixel * 0.5);
    float offset = pixelWorldSize * scale;
	offset = 0.0001;

    float diagScale = offset * sqrt(2.0) * 1;
    float3 offsetBL = -viewRight * diagScale - viewUp * diagScale;
    float3 offsetTR = viewRight * diagScale + viewUp * diagScale;
    float3 offsetBR = viewRight * diagScale - viewUp * diagScale;
    float3 offsetTL = -viewRight * diagScale + viewUp * diagScale;

    float dBL = NormalMap(worldPos + offsetBL);
    float dTR = NormalMap(worldPos + offsetTR);
    float dBR = NormalMap(worldPos + offsetBR);
    float dTL = NormalMap(worldPos + offsetTL);

    float g1 = abs(dBL - dTR);
    float g2 = abs(dTL - dBR);
    float edgeStrength = sqrt(g1 * g1 + g2 * g2);

    return edgeStrength;
}

Varyings Vert (Attributes input)
{
	Varyings output = (Varyings)0;
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_TRANSFER_INSTANCE_ID(input, output);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
	
	VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
	output.positionCS = vertexInput.positionCS;
	output.positionWS = vertexInput.positionWS;
	return output;
}

FragOutput Frag (Varyings input)
{
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
	
    half3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
    Ray ray = CreateRay(input.positionWS, -viewDirWS);

	shapeHitCount = TraverseBvh(_ShapeNodeBuffer, 0, ray.origin, ray.dir, shapeHitIds);
	if (shapeHitCount == 0) discard;

	InsertionSort(shapeHitIds, shapeHitCount);
	bool isHit = Raymarch(ray, _MaxSteps, _MaxDistance, _EpsilonMin, _EpsilonMax);
	//if (!isHit) discard;
	
	float depth = GetNonLinearDepth(ray.hitPoint);
	float3 normal = GetNormal(ray.hitPoint, _EpsilonMin);

	float3 viewNormal = mul((float3x3)UNITY_MATRIX_V, normal) * 0.5 + 0.5;



	 // float3 head = float3(0, 1, 0);
	 // float3 tangent = normalize(cross(normal, abs(normal.y) > 0.99 ? float3(1, 0, 0) : head));
	 // float3 bitangent = cross(normal, tangent);
	 //
	 // float offset = 0.01;
	 // float d1 = NormalMap(ray.hitPoint + tangent * offset);
	 // float d2 = NormalMap(ray.hitPoint - tangent * offset);
	 // float d3 = NormalMap(ray.hitPoint + bitangent * offset);
	 // float d4 = NormalMap(ray.hitPoint - bitangent * offset);
	 //
	 // float edge = (d1 + d2 + d3 + d4) * 0.1;
	 // float outline = smoothstep(0.0, 0.01, edge);
	 // baseColor.rgb = edge.xxx;

	FragOutput output;
	output.color = baseColor;
	output.depth = depth;
	return output;
}

#endif