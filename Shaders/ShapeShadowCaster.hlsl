#ifndef RAYMAN_SHADOWCASTER
#define RAYMAN_SHADOWCASTER

struct Attributes
{
    float4 vertex : POSITION;
    float3 normal : NORMAL;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 posCS : SV_POSITION;
    float3 posWS : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

struct FragOut
{
    float4 color : SV_Target;
    float depth : SV_Depth;
};

Varyings Vert(Attributes input)
{
    Varyings o;
    UNITY_SETUP_INSTANCE_ID(i);
    UNITY_TRANSFER_INSTANCE_ID(i, o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    o.posCS = TransformObjectToHClip(input.vertex.xyz);
    o.posWS = TransformObjectToWorld(input.vertex.xyz);
    return o;
}
			
FragOut Frag(Varyings input)
{
    UNITY_SETUP_INSTANCE_ID(i);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

    Ray ray = CreateRay(input.posWS, GetCameraForward(), 32, 100);
    if (!Raymarch(ray)) discard;
				
    FragOut o;
    o.color = o.depth = GetDepth(ray.hitPoint);
    return o;
}

#endif