#ifndef RAYMAN_BVH
#define RAYMAN_BVH

#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/Aabb.hlsl"

#ifndef STACK_SIZE
#define STACK_SIZE 32
#endif

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
            int shapeIndex = -(node.skipIndex + 1);
            int k = count;
            while (k > 0 && hitIds[k - 1] > shapeIndex)
            {
                hitIds[k] = hitIds[k - 1];
                k--;
            }
            hitIds[k] = shapeIndex;
            count++;

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
            int shapeIndex = -(node.skipIndex + 1);
            int k = count.x;
            while (k > 0 && hitIds[k - 1] > shapeIndex)
            {
                hitIds[k] = hitIds[k - 1];
                k--;
            }
            hitIds[k] = shapeIndex;
            count.x++;

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

#endif