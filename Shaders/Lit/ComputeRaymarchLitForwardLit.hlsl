#ifndef RAYMAN_COMPUTE_FORWARDLIT
#define RAYMAN_COMPUTE_FORWARDLIT

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.davidkimighty.rayman/Shaders/Library/Camera.hlsl"
#include "Packages/com.davidkimighty.rayman/Shaders/Library/Lighting.hlsl"

struct appdata
{
    float4 vertex : POSITION;
    float3 normal : NORMAL;
    float4 tangent : TANGENT;
    float2 texcoord : TEXCOORD0;
    float2 lightmapUV : TEXCOORD1;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct v2f
{
    float4 posCS : SV_POSITION;
    float4 posSS : TEXCOORD0;
    float3 posWS : TEXCOORD1;
    float3 normalWS : TEXCOORD2;
    DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 3);
    half4 fogFactorAndVertexLight : TEXCOORD4;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

struct output
{
    float4 color : SV_Target;
    float depth : SV_Depth;
};

float _ShadowBiasVal;
float _F0;
float _SpecularPow;
float4 _RimColor;
float _RimPow;

v2f vert (appdata v)
{
    v2f o = (v2f)0;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_TRANSFER_INSTANCE_ID(v, o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    
    o.posCS = TransformObjectToHClip(v.vertex.xyz);
    o.posWS = TransformObjectToWorld(v.vertex.xyz);
    o.normalWS = TransformObjectToWorldNormal(v.normal);
    
    half3 vertexLight = VertexLighting(o.posWS, o.normalWS);
    half fogFactor = ComputeFogFactor(o.posCS.z);
    o.fogFactorAndVertexLight = half4(fogFactor, vertexLight);

    OUTPUT_LIGHTMAP_UV(v.lightmapUV, unity_LightmapST, o.lightmapUV);
    OUTPUT_SH(o.normalWS.xyz, o.vertexSH);
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

    const float3 cameraPos = GetCameraPosition();
    float lengthToSurface = length(i.posWS - cameraPos);
    const float depth = result.travelDistance - lengthToSurface < EPSILON ?
        GetDepth(i.posWS) : GetDepth(result.hitPoint);
    
    const float3 viewDir = normalize(cameraPos - result.hitPoint);
    const float fresnel = GetFresnelSchlick(viewDir, result.normal, _F0);
    
    half3 shade = MainLightShade(result.hitPoint, result.rayDirection, _ShadowBiasVal, result.normal, fresnel, _SpecularPow);
    AdditionalLightsShade(result.hitPoint, result.rayDirection, result.normal, fresnel, _SpecularPow, shade);
    shade += RimLightShade(result.normal, viewDir, _RimPow, _RimColor);

    float4 finalColor = result.color;
    finalColor.rgb *= shade + SAMPLE_GI(i.lightmapUV, i.vertexSH, result.normal);
    finalColor.rgb = MixFog(finalColor.rgb, i.fogFactorAndVertexLight.x);
    
    output o;
    o.color = finalColor;
    o.depth = depth;
    return o;
}

#endif