#ifndef RAYMAN_COMPUTE_FORWARDLIT
#define RAYMAN_COMPUTE_FORWARDLIT

#include "Packages/com.davidkimighty.rayman/Shaders/Library/Camera.hlsl"
#include "Packages/com.davidkimighty.rayman/Shaders/Library/Lighting.hlsl"

struct appdata
{
    float4 vertex : POSITION;
    float2 uv : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct v2f
{
    float4 posCS : SV_POSITION;
    float3 posWS : TEXCOORD0;
    DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 1);
    half4 fogFactorAndVertexLight : TEXCOORD2;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

struct output
{
    float4 color : SV_Target;
    float depth : SV_Depth;
};
			
v2f vert (appdata v)
{
    v2f o = (v2f)0;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_TRANSFER_INSTANCE_ID(v, o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    o.posCS = TransformObjectToHClip(v.vertex.xyz);
    o.posWS = TransformObjectToWorld(v.vertex.xyz);
    return o;
}

output frag (v2f i)
{
    UNITY_SETUP_INSTANCE_ID(i);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
    
    const float2 screenPos = GetScreenPosition(i.posCS);
    const uint2 pixelCoord = uint2(screenPos * _ScreenParams.xy);
    const RaymarchResult result = _ResultBuffer[pixelCoord.x + pixelCoord.y * _ScreenParams.x];
				
    if (result.lastHitDistance > EPSILON) discard;
				
    const float depth = GetDepth(i.posWS);
    const float3 viewDir = normalize(GetCameraPosition() - result.hitPoint);
    const float fresnel = GetFresnelSchlick(viewDir, result.normal);
    
    half3 shade = MainLightShade(result.hitPoint, result.rayDirection, result.normal, fresnel);
    AdditionalLightsShade(result.hitPoint, result.rayDirection, result.normal, fresnel, shade);
    shade += RimLightShade(result.normal, viewDir);

    float4 color = result.color;
    color.rgb *= shade + SAMPLE_GI(i.lightmapUV, i.vertexSH, result.normal);
    color.rgb = MixFog(color.rgb, i.fogFactorAndVertexLight.x);
    
    output o;
    o.color = color;
    o.depth = depth;
    return o;
}

#endif