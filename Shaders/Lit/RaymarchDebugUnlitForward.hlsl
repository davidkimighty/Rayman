#ifndef RAYMAN_DEBUG_FORWARD
#define RAYMAN_DEBUG_FORWARD

#include "Packages/com.davidkimighty.rayman/Shaders/Library/Camera.hlsl"
#include "Packages/com.davidkimighty.rayman/Shaders/Library/Geometry.hlsl"

#define B float3(0.0, 0.3, 1.0)
#define Y float3(1.0, 0.8, 0.0)

struct Attributes
{
    float4 vertex : POSITION;
    float3 normal : NORMAL;
};

struct Varyings
{
    float4 posCS : SV_POSITION;
    float3 posWS : TEXCOORD0;
};

struct FragOutput
{
    float4 color : SV_Target;
    float depth : SV_Depth;
};

Varyings Vert (Attributes input)
{
    Varyings output = (Varyings)0;
	output.posCS = TransformObjectToHClip(input.vertex.xyz);
	output.posWS = TransformObjectToWorld(input.vertex.xyz);
    return output;
}

FragOutput Frag (Varyings input)
{
	const float3 cameraPos = GetCameraPosition();
	const float3 rayDir = normalize(input.posWS - cameraPos);
	Ray ray = CreateRay(input.posWS, rayDir, _MaxSteps, _MaxDistance);
	ray.distanceTravelled = length(ray.hitPoint - cameraPos);
	
	TraverseTree(0, ray, hitIds, hitCount);
	InsertionSort(hitIds, hitCount.x);

	int raymarchCount;
	bool rayHit = RaymarchHitCount(ray, raymarchCount);
	
	const float3 normal = GetNormal(ray.hitPoint);
	float lengthToSurface = length(input.posWS - cameraPos);
	const float depth = ray.distanceTravelled - lengthToSurface < EPSILON ?
		GetDepth(input.posWS) : GetDepth(ray.hitPoint);

	FragOutput output;
	output.color = finalColor;
	output.depth = depth;
	
	switch (_DebugMode)
	{
		case 1:
			if (!rayHit) discard;
			break;
		case 2:
			if (!rayHit) discard;
			output.color = float4(normal * 0.5 + 0.5, 1);
			break;
		case 3:
			output.color = float4(GetHitMap(raymarchCount, ray.maxSteps, B, Y), 1);
			output.depth = 1;
			break;
		case 4:
			int total = hitCount.x + hitCount.y;
			float intensity = saturate((float)total / (total + _BoundsDisplayThreshold));
			output.color = 1 * intensity;
			output.depth = 1;
			break;
	}
	return output;
}

#endif