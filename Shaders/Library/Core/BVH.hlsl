#ifndef RAYMAN_BVH
#define RAYMAN_BVH

#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/Aabb.hlsl"

#define STACK_SIZE 32

struct NodeAabb
{
    int id;
    int childIndex;
    Aabb bounds;
};

// Must be implemented by the including shader.
inline NodeAabb GetNode(const int index);

int2 TraverseBvh(const int startIndex, const float3 rayOrigin, const float3 rayDir, inout int hitIds[RAY_MAX_HITS])
{
    int nodeStack[STACK_SIZE];
    int2 count = 0; // count.x is leaf
    int ptr = 0;
    nodeStack[ptr++] = startIndex;
    float3 invDir = rcp(rayDir);

    while (ptr > 0)
    {
        NodeAabb node = GetNode(nodeStack[--ptr]);
        if (!RayIntersect(rayOrigin, invDir, node.bounds)) continue;
        
        if (node.childIndex < 0) // leaf
        {
            hitIds[count.x++] = node.id;
            if (count.x >= RAY_MAX_HITS) break;
        }
        else
        {
            int childL = node.childIndex;
            int childR = childL + 1;
            float dstL = RayIntersectNearDst(rayOrigin, invDir, GetNode(childL).bounds);
            float dstR = RayIntersectNearDst(rayOrigin, invDir, GetNode(childR).bounds);

            bool rightNear = dstL > dstR; 
            nodeStack[ptr++] = rightNear ? childL : childR;
            nodeStack[ptr++] = rightNear ? childR : childL;
            count.y += 2;
        }
    }
    return count;
}

void InsertionSort(inout int hitIds[RAY_MAX_HITS], inout int numHits)
{
    for (int i = 1; i < numHits; i++)
    {
        int key = hitIds[i];
        int j = i - 1;

        while (j >= 0 && hitIds[j] > key)
        {
            hitIds[j + 1] = hitIds[j];
            j -= 1;
        }
        hitIds[j + 1] = key;
    }
}

#endif