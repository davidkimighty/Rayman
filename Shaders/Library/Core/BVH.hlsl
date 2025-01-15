#ifndef RAYMAN_BVH
#define RAYMAN_BVH

#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/AABB.hlsl"

#define STACK_SIZE 32

struct NodeAABB
{
    int id;
    int childIndex;
    AABB bounds;
};

// Must be implemented by the including shader.
inline NodeAABB GetNode(const int index);

inline void TraverseTree(const int startIndex, const Ray ray, inout int hitIds[RAY_MAX_HITS], inout int2 hitCount)
{
    int nodeStack[STACK_SIZE];
    int ptr = hitCount = 0; // count.x is leaf
    nodeStack[ptr++] = startIndex;

    while (ptr > 0)
    {
        NodeAABB node = GetNode(nodeStack[--ptr]);
        if (!RayIntersect(ray, node.bounds)) continue;
        
        if (node.childIndex < 0) // leaf
        {
            hitIds[hitCount.x++] = node.id;
            if (hitCount.x >= RAY_MAX_HITS) break;
        }
        else
        {
            int childL = node.childIndex;
            int childR = childL + 1;
            float dstL = RayIntersectNearDst(ray, GetNode(childL).bounds);
            float dstR = RayIntersectNearDst(ray, GetNode(childR).bounds);

            bool rightNear = dstL > dstR; 
            nodeStack[ptr++] = rightNear ? childL : childR;
            nodeStack[ptr++] = rightNear ? childR : childL;
            hitCount.y += 2;
        }
    }
}

inline void InsertionSort(inout int hitIds[RAY_MAX_HITS], inout int numHits)
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