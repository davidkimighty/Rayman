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

inline float3 GetPosition()
{
    return float3(unity_ObjectToWorld[3].x, unity_ObjectToWorld[3].y, unity_ObjectToWorld[3].z);
}

inline float3 GetScale()
{
    return float3(
        length(float3(unity_ObjectToWorld[0].x, unity_ObjectToWorld[1].x, unity_ObjectToWorld[2].x)),
        length(float3(unity_ObjectToWorld[0].y, unity_ObjectToWorld[1].y, unity_ObjectToWorld[2].y)),
        length(float3(unity_ObjectToWorld[0].z, unity_ObjectToWorld[1].z, unity_ObjectToWorld[2].z)));
}

inline float GetDepth(const float3 posWS)
{
    float4 posCS = TransformWorldToHClip(posWS);
    float z = posCS.z / posCS.w;
    return z;
}

inline float4 ComputeNonStereoScreenPos(float4 pos)
{
    float4 p = pos * 0.5f;
    p.xy = float2(p.x, p.y * _ProjectionParams.x) + p.w;
    p.zw = pos.zw;
    return p;
}

#endif