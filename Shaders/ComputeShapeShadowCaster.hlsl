#ifndef RAYMAN_COMPUTE_SHADOWCASTER
#define RAYMAN_COMPUTE_SHADOWCASTER

#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/Math.hlsl"
#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/SDF.hlsl"
#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/Operation.hlsl"
#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/Distortion.hlsl"
#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/Raymarch.hlsl"
#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/BVH.hlsl"
#include "Packages/com.davidkimighty.rayman/Shaders/Library/Camera.hlsl"
#include "Packages/com.davidkimighty.rayman/Shaders/Library/Geometry.hlsl"

struct appdata
{
    float4 vertex : POSITION;
    float2 uv : TEXCOORD0;
};

struct v2f
{
    float4 posCS : SV_POSITION;
    float3 posWS : TEXCOORD0;
};

struct output
{
    float4 color : SV_Target;
    float depth : SV_Depth;
};

struct Shape
{
    float4x4 transform;
    float3 lossyScale;
    int type;
    float3 size;
    float roundness;
    int operation;
    float smoothness;
    half4 color;
    half4 emissionColor;
    float emissionIntensity;
    int distortionEnabled;
};

struct Distortion
{
    int id;
    int type;
    float amount;
};

int _ShadowMaxSteps;
float _ShadowMaxDistance;
int _DistortionCount;
StructuredBuffer<Shape> _ShapeBuffer;
StructuredBuffer<Distortion> _DistortionBuffer;
int _GroupCount;
StructuredBuffer<int> _RootNodeIndices;
StructuredBuffer<NodeAABB> _NodeBuffer;
        
int2 hitCount;
int hitIds[RAY_MAX_HITS];

inline void ApplyDistortionPositionById(inout float3 pos, const int id)
{
    for (int i = 0; i < _DistortionCount; i++)
    {
        Distortion o = _DistortionBuffer[i];
        if (o.id != id) continue;
				                
        pos = ApplyDistortion(pos, o.type, o.amount);
        break;
    }
}
        
inline float Map(const Ray ray)
{
    float totalDist = _ShadowMaxDistance;
    for (int i = 0; i < hitCount.x; i++)
    {
        Shape shape = _ShapeBuffer[hitIds[i]];
        float3 pos = NormalizeScale(ApplyMatrix(ray.hitPoint, shape.transform), shape.lossyScale);
#ifdef _DISTORTION_FEATURE
        if (shape.distortionEnabled > 0)
            ApplyDistortionPositionById(pos, i);
#endif
        float dist = GetShapeSDF(pos, shape.type, shape.size, shape.roundness);
        float blend = 0;
        totalDist = CombineShapes(totalDist, dist, shape.operation, shape.smoothness, blend);
    }
    return totalDist;
}

inline float NormalMap(const float3 rayPos)
{
    float totalDist = _ShadowMaxDistance;
    return totalDist;
}

inline NodeAABB GetNode(const int index)
{
    return _NodeBuffer[index];
}

v2f vert (appdata v)
{
    v2f o = (v2f)0;
    o.posCS = TransformObjectToHClip(v.vertex.xyz);
    o.posWS = TransformObjectToWorld(v.vertex.xyz);
    return o;
}

output frag (v2f input)
{
    float3 cameraPos = GetCameraPosition();
    Ray ray = CreateRay(input.posWS, GetCameraForward(), _ShadowMaxSteps, _ShadowMaxDistance);
    ray.travelDistance = length(ray.hitPoint - cameraPos);
    
    for (int i = 0; i < _GroupCount; i++)
    {
        NodeAABB rootNode = GetNode(i);
        if (!RayIntersect(ray, rootNode.bounds)) continue;
        TraverseAabbTree(i, ray, hitIds, hitCount);        
    }
    InsertionSort(hitIds, hitCount.x);
    
    if (!Raymarch(ray)) discard;

    float lengthToSurface = length(input.posWS - cameraPos);
    const float depth = ray.travelDistance - lengthToSurface < EPSILON ?
        GetDepth(input.posWS) : GetDepth(ray.hitPoint);

    output output;
    output.color = output.depth = depth;
    return output;
}

#endif