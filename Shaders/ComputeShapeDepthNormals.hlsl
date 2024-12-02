#ifndef RAYMAN_COMPUTE_DEPTHNORMALS
#define RAYMAN_COMPUTE_DEPTHNORMALS

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
    float2 screenPos = GetScreenPosition(i.posCS);
    uint2 pixelCoord = uint2(screenPos * _ScreenParams.xy);
    RaymarchResult result = resultBuffer[pixelCoord.x + pixelCoord.y * _ScreenParams.x];
				
    if (result.lastHitDistance > 0.001) discard;

    output o;
    o.color = o.depth = GetDepth(i.posWS);
    return o;
}

#endif