using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

namespace Rayman
{
    [BurstCompile]
    public struct Bvh
    {
        public NativeArray<AabbNode> Nodes;
        public NativeReference<int> RootIndex;
        public NativeReference<int> NodeCount;
        public NativeReference<int> NextFreeNode;

        public int InsertNode(Aabb bounds, int leafId)
        {
            Span<AabbNode> nodesSpan = Nodes.AsSpan();

            int leafIndex = NextFreeNode.Value++;
            NodeCount.Value++;
            ref AabbNode leafNode = ref nodesSpan[leafIndex];
            leafNode = new AabbNode(bounds, leafId);

            if (RootIndex.Value == -1)
            {
                RootIndex.Value = leafIndex;
                return leafIndex;
            }

            int siblingIndex = FindBestSibling(nodesSpan, bounds);
            ref AabbNode sibling = ref nodesSpan[siblingIndex];
            int oldParentIndex = sibling.Parent;

            int newParentIndex = NextFreeNode.Value++;
            NodeCount.Value++;
            ref AabbNode newParentNode = ref nodesSpan[newParentIndex];
            newParentNode = new AabbNode()
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
                ref AabbNode oldParent = ref nodesSpan[oldParentIndex];
                if (oldParent.LeftChild == siblingIndex)
                    oldParent.LeftChild = newParentIndex;
                else
                    oldParent.RightChild = newParentIndex;
            }
            else
                RootIndex.Value = newParentIndex;

            BalanceAndRefit(nodesSpan, newParentIndex);
            return leafIndex;
        }

        public void RemoveNode(int nodeIndex)
        {
            Span<AabbNode> span = Nodes.AsSpan();

            ref AabbNode toRemove = ref span[nodeIndex];
            toRemove.LeafId = -1;
            toRemove.Parent = -1;

            int parentIdx = toRemove.Parent;
            if (parentIdx == -1)
            {
                RootIndex.Value = -1;
                return;
            }

            ref AabbNode parent = ref span[parentIdx];
            int grandparentIndex = parent.Parent;
            int siblingIndex = (parent.LeftChild == nodeIndex) ? parent.RightChild : parent.LeftChild;

            parent.LeftChild = -1;
            parent.RightChild = -1;
            parent.Parent = -1;

            ref AabbNode sibling = ref span[siblingIndex];

            if (grandparentIndex == -1)
            {
                RootIndex.Value = siblingIndex;
                sibling.Parent = -1;
            }
            else
            {
                ref AabbNode grandparent = ref span[grandparentIndex];
                if (grandparent.LeftChild == parentIdx)
                    grandparent.LeftChild = siblingIndex;
                else
                    grandparent.RightChild = siblingIndex;
                sibling.Parent = grandparentIndex;

                BalanceAndRefit(span, grandparentIndex);
            }
        }

        private int FindBestSibling(Span<AabbNode> span, Aabb newBounds)
        {
            int index = RootIndex.Value;
            while (true)
            {
                ref AabbNode node = ref span[index];
                if (node.IsLeaf) break;

                float area = node.Bounds.HalfArea();
                Aabb combined = Aabb.Union(node.Bounds, newBounds);
                float combinedArea = combined.HalfArea();

                float cost = combinedArea * 2f;
                float inheritedCost = (combinedArea - area) * 2f;

                ref AabbNode leftNode = ref span[node.LeftChild];
                float combinedLeftArea = Aabb.Union(leftNode.Bounds, newBounds).HalfArea();
                if (!leftNode.IsLeaf)
                    combinedLeftArea -= leftNode.Bounds.HalfArea();
                float costLeft = combinedLeftArea + inheritedCost;

                ref AabbNode rightNode = ref span[node.RightChild];
                float combinedRightArea = Aabb.Union(rightNode.Bounds, newBounds).HalfArea();
                if (!rightNode.IsLeaf)
                    combinedRightArea -= rightNode.Bounds.HalfArea();
                float costRight = combinedRightArea + inheritedCost;

                if (cost < costLeft && cost < costRight) break;

                index = costLeft < costRight ? node.LeftChild : node.RightChild;
            }
            return index;
        }

        private void BalanceAndRefit(Span<AabbNode> span, int index)
        {
            while (index != -1)
            {
                index = Balance(span, index);  
                SyncNode(span, index);
                SyncNode(span, index);

                ref AabbNode node = ref span[index];
                index = node.Parent;
            }
        }

        private int Balance(Span<AabbNode> span, int index)
        {
            ref AabbNode node = ref span[index];
            if (node.IsLeaf || node.Height < 2) return index;

            int leftIndex = node.LeftChild;
            int rightIndex = node.RightChild;
            int balanceFactor = span[rightIndex].Height - span[leftIndex].Height;

            if (balanceFactor > 1)
            {
                ref AabbNode nodeRight = ref span[rightIndex];
                if (span[nodeRight.RightChild].Height < span[nodeRight.LeftChild].Height)
                {
                    rightIndex = RotateRight(span, rightIndex);
                    node.RightChild = rightIndex;
                }
                return RotateLeft(span, index);
            }
            else if (balanceFactor < -1)
            {
                ref AabbNode nodeLeft = ref span[leftIndex];
                if (span[nodeLeft.LeftChild].Height < span[nodeLeft.RightChild].Height)
                {
                    leftIndex = RotateLeft(span, leftIndex);
                    node.LeftChild = leftIndex;
                }
                return RotateRight(span, index);
            }
            return index;
        }

        private int RotateLeft(Span<AabbNode> span, int aIndex)
        {
            ref AabbNode a = ref span[aIndex];
            int bIndex = a.RightChild;
            ref AabbNode b = ref span[bIndex];

            a.RightChild = b.LeftChild;
            if (b.LeftChild != -1)
                span[b.LeftChild].Parent = aIndex;

            b.Parent = a.Parent;
            if (a.Parent != -1)
            {
                ref AabbNode ap = ref span[a.Parent];
                if (ap.LeftChild == aIndex) ap.LeftChild = bIndex;
                else ap.RightChild = bIndex;
            }
            else
                RootIndex.Value = bIndex;

            b.LeftChild = aIndex;
            a.Parent = bIndex;

            SyncNode(span, aIndex);
            SyncNode(span, bIndex);
            return bIndex;
        }

        private int RotateRight(Span<AabbNode> span, int aIndex)
        {
            ref AabbNode a = ref span[aIndex];
            int bIndex = a.LeftChild;
            ref AabbNode b = ref span[bIndex];

            a.LeftChild = b.RightChild;
            if (b.RightChild != -1)
                span[b.RightChild].Parent = aIndex;

            b.Parent = a.Parent;
            if (a.Parent != -1)
            {
                ref AabbNode ap = ref span[a.Parent];
                if (ap.LeftChild == aIndex) ap.LeftChild = bIndex;
                else ap.RightChild = bIndex;
            }
            else
                RootIndex.Value = bIndex;

            b.RightChild = aIndex;
            a.Parent = bIndex;

            SyncNode(span, aIndex);
            SyncNode(span, bIndex);
            return bIndex;
        }

        private void SyncNode(Span<AabbNode> span, int index)
        {
            ref AabbNode node = ref span[index];
            ref readonly AabbNode left = ref span[node.LeftChild];
            ref readonly AabbNode right = ref span[node.RightChild];

            node.Height = 1 + math.max(left.Height, right.Height);
            node.Bounds = Aabb.Union(left.Bounds, right.Bounds);
        }
    }
}
