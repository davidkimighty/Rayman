#ifndef RAYMAN_BVH
#define RAYMAN_BVH

#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/AABB.hlsl"

#define STACK_SIZE 64

struct NodeAABB
{
    int id;
    AABB bounds;
    int parent;
    int left;
    int right;
};

#ifndef NODE_BUFFER_DEFINED
StructuredBuffer<NodeAABB> nodeBuffer;
#define NODE_BUFFER_DEFINED
#endif

inline void TraverseAabbTree(const Ray ray, inout int hitIds[RAY_MAX_HITS], inout int numHits)
{
    int stack[STACK_SIZE];
    int ptr = numHits = 0;
    stack[ptr] = 0;

    while (ptr >= 0)
    {
        int nodeIndex = stack[ptr--];
        if (nodeIndex < 0) continue;

        const NodeAABB node = nodeBuffer[nodeIndex];
        int tMin, tMax;
        if (!RayIntersect(ray, node.bounds, tMin, tMax)) continue;
        
        if (node.left < 0)
        {
            hitIds[numHits++] = node.id;
            if (numHits >= RAY_MAX_HITS) break;
        }
        else
        {
            ptr = min(ptr + 1, STACK_SIZE - 1);
            stack[ptr] = node.left;
            ptr = min(ptr + 1, STACK_SIZE - 1);
            stack[ptr] = node.right;
        }
    }

    // perform insertion sort
    for (int i = 1; i < numHits; i++)
    {
        int key = hitIds[i];
        int j = i - 1;

        while (j >= 0 && hitIds[j] > key)
        {
            hitIds[j + 1] = hitIds[j];
            j = j - 1;
        }
        hitIds[j + 1] = key;
    }
}

#endif