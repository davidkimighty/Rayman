#ifndef RAYMAN_SHADOWCASTER
#define RAYMAN_SHADOWCASTER

#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/BVH.hlsl"
#include "Packages/com.davidkimighty.rayman/Shaders/Library/Camera.hlsl"
#include "Packages/com.davidkimighty.rayman/Shaders/Library/Geometry.hlsl"

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

StructuredBuffer<NodeAABB> _NodeBuffer;

inline NodeAABB GetNode(const int index)
{
    return _NodeBuffer[index];
}

Varyings Vert(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    output.posCS = TransformObjectToHClip(input.vertex.xyz);
    output.posWS = TransformObjectToWorld(input.vertex.xyz);
    return output;
}

FragOut Frag(Varyings input)
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    float3 cameraPos = GetCameraPosition();
    Ray ray = CreateRay(input.posWS, GetCameraForward(), 32, 100);
    ray.travelDistance = length(ray.hitPoint - cameraPos);
    
    TraverseAabbTree(0, ray, hitIds, hitCount);
    InsertionSort(hitIds, hitCount.x);
    
    if (!Raymarch(ray)) discard;

    float lengthToSurface = length(input.posWS - cameraPos);
    const float depth = ray.travelDistance - lengthToSurface < EPSILON ?
        GetDepth(input.posWS) : GetDepth(ray.hitPoint);

    FragOut output;
    output.color = output.depth = depth;
    return output;
}

#endif