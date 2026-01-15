#ifndef RAYMAN_BVH
#define RAYMAN_BVH

#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/Aabb.hlsl"

#define STACK_SIZE 32

struct NodeAabb
{
    float3 min;
    float3 max;
    int skipIndex;
};

int TraverseBvh(StructuredBuffer<NodeAabb> buffer, float3 rayOrigin, float3 rayInvDir, inout int hitIds[RAY_MAX_HITS])
{
    int nodeStack[STACK_SIZE];
    int count = 0;
    int ptr = 0;
    nodeStack[ptr++] = 0;

    while (ptr > 0)
    {
        int currentIndex = nodeStack[--ptr];
        NodeAabb node = buffer[currentIndex];
        if (!RayIntersect(rayOrigin, rayInvDir, node.min, node.max)) continue;
       
        if (node.skipIndex < 0) // leaf
        {
            hitIds[count++] = -(node.skipIndex + 1);
            if (count >= RAY_MAX_HITS) break;
        }
        else
        {
            int leftIndex = currentIndex + 1;
            NodeAabb leftNode = buffer[leftIndex];
            float dstLeft;
            RayIntersect(rayOrigin, rayInvDir, leftNode.min, leftNode.max, dstLeft);
            
            int rightIndex = currentIndex + node.skipIndex;
            NodeAabb rightNode = buffer[rightIndex];
            float dstRight;
            RayIntersect(rayOrigin, rayInvDir, rightNode.min, rightNode.max, dstRight);
           
            bool rightNear = dstLeft > dstRight;
            nodeStack[ptr++] = rightNear ? leftIndex : rightIndex;
            nodeStack[ptr++] = rightNear ? rightIndex : leftIndex;
        }
    }
    return count;
}

int2 TraverseBvhCount(StructuredBuffer<NodeAabb> buffer, float3 rayOrigin, float3 rayInvDir, inout int hitIds[RAY_MAX_HITS])
{
    int nodeStack[STACK_SIZE];
    int2 count = 0;
    int ptr = 0;
    nodeStack[ptr++] = 0;

    while (ptr > 0)
    {
        int currentIndex = nodeStack[--ptr];
        NodeAabb node = buffer[currentIndex];
        if (!RayIntersect(rayOrigin, rayInvDir, node.min, node.max)) continue;
        
        if (node.skipIndex < 0) // leaf
        {
            hitIds[count.x++] = -(node.skipIndex + 1);
            if (count.x >= RAY_MAX_HITS) break;
        }
        else
        {
            int leftIndex = currentIndex + 1;
            NodeAabb leftNode = buffer[leftIndex];
            float dstLeft;
            RayIntersect(rayOrigin, rayInvDir, leftNode.min, leftNode.max, dstLeft);
            
            int rightIndex = currentIndex + node.skipIndex;
            NodeAabb rightNode = buffer[rightIndex];
            float dstRight;
            RayIntersect(rayOrigin, rayInvDir, rightNode.min, rightNode.max, dstRight);

            bool rightNear = dstLeft > dstRight;
            nodeStack[ptr++] = rightNear ? leftIndex : rightIndex;
            nodeStack[ptr++] = rightNear ? rightIndex : leftIndex;
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