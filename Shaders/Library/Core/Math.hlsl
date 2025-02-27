#ifndef RAYMAN_MATH
#define RAYMAN_MATH

inline float3 GetScale(const float4x4 transform)
{
    return float3(length(transform[0].xyz), length(transform[1].xyz), length(transform[2].xyz));
}

inline float GetDepth(const float3 posWS, const float4x4 viewProj)
{
    float4 posCS = mul(viewProj, float4(posWS, 1.0));
    float z = posCS.z / posCS.w;
    return z;
}

inline float Sigmoid(float x, float k)
{
    return 1.0 / (1.0 + exp(-k * (x - 0.5)));
}

#endif