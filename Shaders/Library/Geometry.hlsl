#ifndef RAYMAN_GEOMETRY
#define RAYMAN_GEOMETRY

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

inline float2 GetScreenPosition(const float2 posCS)
{
#if UNITY_UV_STARTS_AT_TOP
    float2 pixelPos = float2(posCS.x, (_ProjectionParams.x < 0) ? (_ScreenParams.y - posCS.y) : posCS.y);
#else
    float2 pixelPos = float2(posCS.x, (_ProjectionParams.x > 0) ? (_ScreenParams.y - posCS.y) : posCS.y);
#endif
    float2 ndcPos = pixelPos.xy / _ScreenParams.xy;
    ndcPos.y = 1.0f - ndcPos.y;
    float4 pos = float4(ndcPos.xy, 0, 0);
    float2 screenPos = all(isfinite(pos)) ? half4(pos.x, pos.y, pos.z, 1.0) : float4(1.0f, 0.0f, 1.0f, 1.0f);
    return screenPos;
}

inline float2 GetMatCap(const float3 viewDir, const float3 normal)
{
    float3 reflected = reflect(viewDir, normal);
    float m = 2.8284271247461903 * sqrt(reflected.z + 1.0);
    float2 uv = reflected.xy / m + 0.5;
    uv.y = 1.0 - uv.y;
    return uv;
}

#endif