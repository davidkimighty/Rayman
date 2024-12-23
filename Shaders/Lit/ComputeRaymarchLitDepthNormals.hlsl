#ifndef RAYMAN_COMPUTE_DEPTHNORMALS
#define RAYMAN_COMPUTE_DEPTHNORMALS

#include "Packages/com.davidkimighty.rayman/Shaders/Library/Camera.hlsl"

struct appdata
{
    float4 vertex : POSITION;
    float2 uv : TEXCOORD0;
};

struct v2f
{
    float4 posCS : SV_POSITION;
    float3 posWS : TEXCOORD0;
};

struct output
{
    float4 color : SV_Target;
    float depth : SV_Depth;
};
			
v2f vert (appdata v)
{
    v2f o = (v2f)0;
    o.posCS = TransformObjectToHClip(v.vertex.xyz);
    o.posWS = TransformObjectToWorld(v.vertex.xyz);
    return o;
}

output frag (v2f i)
{
    const float2 screenPos = GetScreenPosition(i.posCS);
    const uint2 pixelCoord = uint2(screenPos * _ScreenParams.xy);
    const RaymarchResult result = _ResultBuffer[pixelCoord.x + pixelCoord.y * _ScreenParams.x];
				
    if (result.lastHitDistance > EPSILON) discard;

    const float3 cameraPos = GetCameraPosition();
    float lengthToSurface = length(i.posWS - cameraPos);
    const float depth = result.travelDistance - lengthToSurface < EPSILON ?
        GetDepth(i.posWS) : GetDepth(result.hitPoint);

    output o;
    o.color = o.depth = depth;
    return o;
}

#endif