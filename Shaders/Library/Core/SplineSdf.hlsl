#ifndef RAYMAN_SPLINE_SDF
#define RAYMAN_SPLINE_SDF

inline float SegmentSdf(float3 p, float3 a, float3 b, out float h)
{
    float3 pa = p - a, ba = b - a;
    h = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
    return length(pa - ba * h);
}

inline float ThickLine(float d, float t, float ra, float rb)
{
    return d - lerp(ra, rb, smoothstep(0.0, 1.0, t));
}

inline float Determinant(float2 a, float2 b)
{
    return a.x * b.y - a.y * b.x;
}

inline float3 ClosestPoint(float2 b0, float2 b1, float2 b2)
{
    float a = Determinant(b0, b2);
    float b = 2.0 * Determinant(b1, b0);
    float d = 2.0 * Determinant(b2, b1);
    float f = b * d - a * a;
    float2 d21 = b2 - b1;
    float2 d10 = b1 - b0;
    float2 d20 = b2 - b0;
    float2 gf = 2.0 * (b * d21 + d * d10 + a * d20);
    gf = float2(gf.y, -gf.x);
    float2 pp = -f * gf / dot(gf, gf);
    float2 d0p = b0 - pp;
    float2 ap = Determinant(d0p, d20);
    float bp = 2.0 * Determinant(d10, d0p);
    float t = clamp((ap + bp) / (2.0 * a + b + d), 0.0, 1.0);
    return float3(lerp(lerp(b0, b1, t), lerp(b1, b2, t), t), t);
}

inline float2 QuadraticBezierSdf(float3 p, float3 a, float3 b, float3 c, out float3 pos)
{
    float3 w = normalize(cross(c - b, a - b));
    float3 u = normalize(c - b);
    float3 v = normalize(cross(w, u));

    float2 a2 = float2(dot(a - b, u), dot(a - b, v));
    float2 b2 = float2(0.0, 0.0);
    float2 c2 = float2(dot(c - b, u), dot(c - b, v));
    float3 p3 = float3(dot(p - b, u), dot(p - b, v), dot(p - b, w));

    float3 cp = ClosestPoint(a2 - p3.xy, b2 - p3.xy, c2 - p3.xy);
    //pos = b + cp.x * u + cp.y * v;
    pos = b + cp.x * u + cp.y * v + cp.z * w;
    return float2(sqrt(dot(cp.xy, cp.xy) + p3.z * p3.z), cp.z);
}

inline float3 GetBezierPoint(float t, float3 p0, float3 p1, float3 p2, float3 p3)
{
    float it = 1 - t;
    return p0 * (it * it * it) +
        p1 * (3 * it * it * t) +
        p2 * (3 * it * t * t) +
        p3 * (t * t * t);
}

inline float CubicBezierSegmentSdf(float3 pos, float3 p0, float3 p1, float3 p2, float3 p3,
    out float resultT, const int subdiv = 16)
{
    float dist03 = distance(p0, p3);
    float controlDist = distance(p0, p1) + distance(p1, p2) + distance(p2, p3);
    
    if (controlDist <= dist03 + 0.001) 
    {
        float3 pa = pos - p0;
        float3 ba = p3 - p0;
        resultT = saturate(dot(pa, ba) / max(dot(ba, ba), 1e-6));
        return length(pa - ba * resultT);
    }
    
    float minDistSq = 100;
    float bestT = 0;
    float3 prevP = p0;
    const float iSubdiv = 1.0 / float(subdiv);

    [loop]
    for (int i = 1; i <= subdiv; i++)
    {
        float t = float(i) * iSubdiv;
        float3 currentP = GetBezierPoint(t, p0, p1, p2, p3);

        float3 pa = pos - prevP;
        float3 ba = currentP - prevP;
        float h = saturate(dot(pa, ba) / max(dot(ba, ba), 1e-6));
        
        float3 diff = pa - ba * h;
        float distSq = dot(diff, diff);

        if (distSq < minDistSq)
        {
            minDistSq = distSq;
            bestT = float(i - 1) * iSubdiv + h * iSubdiv;
        }
        prevP = currentP;
    }

    if (bestT > 0.1 && bestT < 0.9)
    {
        float delta = iSubdiv * 0.5;
        float tM = bestT - delta;
        float tP = bestT + delta;

        float3 pM = GetBezierPoint(tM, p0, p1, p2, p3);
        float3 pP = GetBezierPoint(tP, p0, p1, p2, p3);
        float dM = dot(pos - pM, pos - pM);
        float dP = dot(pos - pP, pos - pP);

        float denom = 2.0 * (dM + dP - 2.0 * minDistSq);
        if (abs(denom) > 1e-6) 
            bestT = saturate(bestT + delta * (dM - dP) / denom);
    }
    resultT = saturate(bestT);
    float3 smoothP = GetBezierPoint(resultT, p0, p1, p2, p3);
    return distance(pos, smoothP);
}

#endif