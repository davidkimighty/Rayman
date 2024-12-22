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

// Must be implemented by the including shader.
inline NodeAABB GetNode(const int index);

inline void TraverseAabbTree(const int startIndex, const Ray ray, inout int hitIds[RAY_MAX_HITS], inout int2 hitCount)
{
    int stack[STACK_SIZE];
    int ptr = hitCount = 0; // count.x is leaf
    stack[ptr] = startIndex;

    while (ptr >= 0)
    {
        int nodeIndex = stack[ptr--];
        if (nodeIndex < 0) continue;

        const NodeAABB node = GetNode(nodeIndex);
        if (!RayIntersect(ray, node.bounds)) continue;
        
        if (node.left < 0) // leaf
        {
            hitIds[hitCount.x++] = node.id;
            if (hitCount.x >= RAY_MAX_HITS) break;
        }
        else
        {
            ptr = min(ptr + 1, STACK_SIZE - 1);
            stack[ptr] = node.left;
            ptr = min(ptr + 1, STACK_SIZE - 1);
            stack[ptr] = node.right;
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
            j = j - 1;
        }
        hitIds[j + 1] = key;
    }
}

#endif