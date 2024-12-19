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
StructuredBuffer<NodeAABB> _NodeBuffer;
#define NODE_BUFFER_DEFINED
#endif

inline void TraverseAabbTree(const Ray ray, inout int hitIds[RAY_MAX_HITS], inout int2 count)
{
    int stack[STACK_SIZE];
    int ptr = count = 0; // count.x is leaf
    stack[ptr] = 0;

    while (ptr >= 0)
    {
        int nodeIndex = stack[ptr--];
        if (nodeIndex < 0) continue;

        const NodeAABB node = _NodeBuffer[nodeIndex];
        float dstFar, dstNear;
        if (!RayIntersect(ray, node.bounds, dstNear, dstFar)) continue;
        
        if (node.left < 0) // leaf
        {
            hitIds[count.x++] = node.id;
            if (count.x >= RAY_MAX_HITS) break;
        }
        else
        {
            ptr = min(ptr + 1, STACK_SIZE - 1);
            stack[ptr] = node.left;
            ptr = min(ptr + 1, STACK_SIZE - 1);
            stack[ptr] = node.right;
            count.y += 2;
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
            j = j - 1;
        }
        hitIds[j + 1] = key;
    }
}

#endif