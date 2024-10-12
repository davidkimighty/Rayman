#ifndef RAYMAN_TRANSFORMSPACE
#define RAYMAN_TRANSFORMSPACE

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

inline float3 ToObject(const float3 pos)
{
    return mul(unity_WorldToObject, float4(pos, 1.)).xyz;
}

inline float3 ToWorld(const float3 pos)
{
    return mul(unity_ObjectToWorld, float4(pos, 1.)).xyz;
}

inline float3 GetScale()
{
    return float3(
        length(float3(unity_ObjectToWorld[0].x, unity_ObjectToWorld[1].x, unity_ObjectToWorld[2].x)),
        length(float3(unity_ObjectToWorld[0].y, unity_ObjectToWorld[1].y, unity_ObjectToWorld[2].y)),
        length(float3(unity_ObjectToWorld[0].z, unity_ObjectToWorld[1].z, unity_ObjectToWorld[2].z)));
}

inline float GetDepth(const float3 wsPos)
{
    float4 csPos = TransformWorldToHClip(wsPos);
    float z = csPos.z / csPos.w;
    return z;
}

#endif