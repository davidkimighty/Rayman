#ifndef RAYMAN_DEBUG
#define RAYMAN_DEBUG

#ifdef DEBUG_MODE

#define B float3(0.0, 0.3, 1.0)
#define Y float3(1.0, 0.8, 0.0)

int _DebugMode;
int _BoundsDisplayThreshold;

inline half4 DebugRaymarch(float3 posWS, float3 cameraPos,
    Ray ray, int maxSteps, int maxDistance, float2 epsilon, out float depth)
{
    int raymarchCount;
    bool rayHit = RaymarchHitCount(ray, maxSteps, maxDistance, epsilon, raymarchCount);
    depth = 1;
	
    switch (_DebugMode)
    {
        case 1:
            if (!rayHit) discard;
            depth = ray.distanceTravelled - length(posWS - cameraPos) < ray.epsilon ?
                GetDepth(posWS) : GetDepth(ray.hitPoint);
            return half4(GetNormal(ray.hitPoint, ray.epsilon) * 0.5 + 0.5, 1);
        case 2:
            return half4(GetHitMap(raymarchCount, maxSteps, B, Y), 1);
        case 3:
            int total = hitCount.x + hitCount.y;
            return 1 * saturate((float)total / (total + _BoundsDisplayThreshold));
    }
    return 0;
}

#endif

#endif