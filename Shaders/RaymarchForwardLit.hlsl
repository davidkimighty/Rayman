#ifndef RAYMAN_FORWARDLIT
#define RAYMAN_FORWARDLIT

#include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

#include "Packages/com.davidkimighty.rayman/Shaders/Library/Raymarching.hlsl"
#include "Packages/com.davidkimighty.rayman/Shaders/Library/Lighting.hlsl"

struct Attributes
{
    float4 vertex : POSITION;
    float3 normal : NORMAL;
    float4 tangent : TANGENT;
    float2 texcoord : TEXCOORD0;
    float2 lightmapUV : TEXCOORD1;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
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

struct FragOutput
{
    float4 color : SV_Target;
    float depth : SV_Depth;
};

int _MaxSteps;
float _MaxDist;

Varyings Vert (Attributes input)
{
    Varyings output = (Varyings)0;
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_TRANSFER_INSTANCE_ID(input, output);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

	VertexPositionInputs vertexInput = GetVertexPositionInputs(input.vertex.xyz);
	output.posCS = vertexInput.positionCS;
	output.posWS = vertexInput.positionWS;
	output.normalWS = TransformObjectToWorldNormal(input.normal);

	half3 vertexLight = VertexLighting(output.posWS, output.normalWS);
	half fogFactor = ComputeFogFactor(output.posCS.z);
	output.fogFactorAndVertexLight = half4(fogFactor, vertexLight);

	OUTPUT_LIGHTMAP_UV(input.lightmapUV, unity_LightmapST, output.lightmapUV);
	OUTPUT_SH(output.normalWS.xyz, output.vertexSH);
    return output;
}

FragOutput Frag (Varyings input)
{
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

	half4 color = 0;
	Ray ray = InitRay(input.posWS, _MaxSteps, _MaxDist);
    if (!Raymarch(ray, color)) discard;
	
	const float3 normal = GetNormal(ray.travelledPoint);
	const float depth = GetDepth(ray, input.posWS);
	const float3 viewDir = normalize(GetCameraPosition() - ray.travelledPoint);
	const float fresnel = GetFresnelSchlick(viewDir, normal);
	
	half3 shade = MainLightShade(ray.travelledPoint, ray.dir, normal, fresnel);
	AdditionalLightsShade(ray.travelledPoint, ray.dir, normal, fresnel, shade);
	shade += RimLightShade(normal, viewDir);
	
	color.rgb *= shade + SAMPLE_GI(input.lightmapUV, input.vertexSH, normal);
	color.rgb = MixFog(color.rgb, input.fogFactorAndVertexLight.x);
	
	FragOutput output;
	output.color = color;
	output.depth = depth;
	return output;
}

#endif