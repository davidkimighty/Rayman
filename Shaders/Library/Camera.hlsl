#ifndef RAYMAN_CAMERA
#define RAYMAN_CAMERA

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

inline float3 GetCameraPosition()
{
    return UNITY_MATRIX_I_V._m03_m13_m23;
}

inline float3 GetCameraForward()
{
    return -UNITY_MATRIX_V[2].xyz;
}

inline float GetCameraNearClip()
{
    return _ProjectionParams.y;
}

inline float GetCameraFarClip()
{
    return _ProjectionParams.z;
}

inline bool IsCameraPerspective()
{
    return any(UNITY_MATRIX_P[3].xyz);
}

#endif