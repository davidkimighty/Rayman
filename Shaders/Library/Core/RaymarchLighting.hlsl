#ifndef RAYMAN_RAYMARCH_LIGHTING
#define RAYMAN_RAYMARCH_LIGHTING

float NormalMap(const float3 positionWS);

float3 GetNormal(const float3 positionWS, const float epsilon)
{
    float3 x = float3(epsilon, 0, 0);
    float3 y = float3(0, epsilon, 0);
    float3 z = float3(0, 0, epsilon);

    float distX = NormalMap(positionWS + x) - NormalMap(positionWS - x);
    float distY = NormalMap(positionWS + y) - NormalMap(positionWS - y);
    float distZ = NormalMap(positionWS + z) - NormalMap(positionWS - z);
    return normalize(float3(distX, distY, distZ));
}

#endif