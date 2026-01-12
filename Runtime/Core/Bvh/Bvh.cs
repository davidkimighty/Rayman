using System;
using Unity.Collections;
using Unity.Mathematics;

namespace Rayman
{
    public struct Bvh
    {
        public NativeArray<BvhNode> Nodes;
        private int rootIndex;
        private int nodeCount;
        private int nextFreeNode;

        public void InsertNode(Aabb bounds, int leafId)
        {
            Span<BvhNode> nodesSpan = Nodes.AsSpan();

            int leafIndex = nextFreeNode++;
            nodeCount++;
            ref BvhNode leafNode = ref nodesSpan[leafIndex];
            leafNode = new BvhNode(bounds, leafId);

            if (rootIndex == -1)
            {
                rootIndex = leafIndex;
                return;
            }

            int siblingIndex = FindBestSibling(nodesSpan, bounds);
            ref BvhNode sibling = ref nodesSpan[siblingIndex];
            int oldParentIndex = sibling.Parent;

            int newParentIndex = nextFreeNode++;
            nodeCount++;
            ref BvhNode newParentNode = ref nodesSpan[newParentIndex];
            newParentNode = new BvhNode()
            {
                Bounds = Aabb.Union(Nodes[siblingIndex].Bounds, bounds),
                Parent = oldParentIndex,
                LeftChild = siblingIndex,
                RightChild = leafIndex,
                Height = 1 + sibling.Height,
                LeafId = -1
            };

            sibling.Parent = newParentIndex;
            leafNode.Parent = newParentIndex;

            if (oldParentIndex != -1)
            {
                ref BvhNode oldParent = ref nodesSpan[oldParentIndex];
                if (oldParent.LeftChild == siblingIndex)
                    oldParent.LeftChild = newParentIndex;
                else
                    oldParent.RightChild = newParentIndex;
            }
            else
                rootIndex = newParentIndex;

            BalanceAndRefit(nodesSpan, newParentIndex);
        }

        private int FindBestSibling(Span<BvhNode> span, Aabb newBounds)
        {
            int index = rootIndex;
            while (true)
            {
                ref BvhNode node = ref span[index];
                if (node.IsLeaf) break;

                float area = node.Bounds.HalfArea();
                Aabb combined = Aabb.Union(node.Bounds, newBounds);
                float combinedArea = combined.HalfArea();

                float cost = combinedArea * 2f;
                float inheritedCost = (combinedArea - area) * 2f;

                ref BvhNode leftNode = ref span[node.LeftChild];
                float combinedLeftArea = Aabb.Union(leftNode.Bounds, newBounds).HalfArea();
                if (!leftNode.IsLeaf)
                    combinedLeftArea -= leftNode.Bounds.HalfArea();
                float costLeft = combinedLeftArea + inheritedCost;

                ref BvhNode rightNode = ref span[node.RightChild];
                float combinedRightArea = Aabb.Union(rightNode.Bounds, newBounds).HalfArea();
                if (!rightNode.IsLeaf)
                    combinedRightArea -= rightNode.Bounds.HalfArea();
                float costRight = combinedRightArea + inheritedCost;

                if (cost < costLeft && cost < costRight) break;

                index = costLeft < costRight ? node.LeftChild : node.RightChild;
            }
            return index;
        }

        private void BalanceAndRefit(Span<BvhNode> span, int index)
        {
            while (index != -1)
            {
                index = Balance(span, index);  
                SyncNode(span, index);
                SyncNode(span, index);

                ref BvhNode node = ref span[index];
                index = node.Parent;
            }
        }

        private int Balance(Span<BvhNode> span, int index)
        {
            ref BvhNode node = ref span[index];
            if (node.IsLeaf || node.Height < 2) return index;

            int leftIndex = node.LeftChild;
            int rightIndex = node.RightChild;
            int balanceFactor = span[rightIndex].Height - span[leftIndex].Height;

            if (balanceFactor > 1)
            {
                ref BvhNode nodeRight = ref span[rightIndex];
                if (span[nodeRight.RightChild].Height < span[nodeRight.LeftChild].Height)
                {
                    rightIndex = RotateRight(span, rightIndex);
                    node.RightChild = rightIndex;
                }
                return RotateLeft(span, index);
            }
            else if (balanceFactor < -1)
            {
                ref BvhNode nodeLeft = ref span[leftIndex];
                if (span[nodeLeft.LeftChild].Height < span[nodeLeft.RightChild].Height)
                {
                    leftIndex = RotateLeft(span, leftIndex);
                    node.LeftChild = leftIndex;
                }
                return RotateRight(span, index);
            }
            return index;
        }

        private int RotateLeft(Span<BvhNode> span, int aIndex)
        {
            ref BvhNode a = ref span[aIndex];
            int bIndex = a.RightChild;
            ref BvhNode b = ref span[bIndex];

            a.RightChild = b.LeftChild;
            if (b.LeftChild != -1)
                span[b.LeftChild].Parent = aIndex;

            b.Parent = a.Parent;
            if (a.Parent != -1)
            {
                ref BvhNode ap = ref span[a.Parent];
                if (ap.LeftChild == aIndex) ap.LeftChild = bIndex;
                else ap.RightChild = bIndex;
            }
            else
                rootIndex = bIndex;

            b.LeftChild = aIndex;
            a.Parent = bIndex;

            SyncNode(span, aIndex);
            SyncNode(span, bIndex);
            return bIndex;
        }

        private int RotateRight(Span<BvhNode> span, int aIndex)
        {
            ref BvhNode a = ref span[aIndex];
            int bIndex = a.LeftChild;
            ref BvhNode b = ref span[bIndex];

            a.LeftChild = b.RightChild;
            if (b.RightChild != -1)
                span[b.RightChild].Parent = aIndex;

            b.Parent = a.Parent;
            if (a.Parent != -1)
            {
                ref BvhNode ap = ref span[a.Parent];
                if (ap.LeftChild == aIndex) ap.LeftChild = bIndex;
                else ap.RightChild = bIndex;
            }
            else
                rootIndex = bIndex;

            b.RightChild = aIndex;
            a.Parent = bIndex;

            SyncNode(span, aIndex);
            SyncNode(span, bIndex);
            return bIndex;
        }

        private void SyncNode(Span<BvhNode> span, int index)
        {
            ref BvhNode node = ref span[index];
            ref readonly BvhNode left = ref span[node.LeftChild];
            ref readonly BvhNode right = ref span[node.RightChild];

            node.Height = 1 + math.max(left.Height, right.Height);
            node.Bounds = Aabb.Union(left.Bounds, right.Bounds);
        }
    }
}
