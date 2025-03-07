using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rayman
{
    public class Bvh<T> : ISpatialStructure<T> where T : struct, IBounds<T>
    {
        public SpatialNode<T> Root { get; private set; }
        public int Count => SpatialNode<T>.GetNodesCount(Root);
        public int MaxHeight { get; private set; }
        
        public Bvh(T[] bounds)
        {
            for (int i = 0; i < bounds.Length; i++)
                AddLeafNode(i, bounds[i]);
        }

        public Bvh(T[] bounds, int[] ids)
        {
            if (bounds.Length != ids.Length) return;
            
            for (int i = 0; i < bounds.Length; i++)
                AddLeafNode(ids[i], bounds[i]);
        }
        
        public void AddLeafNode(int id, T bounds)
        {
            SpatialNode<T> nodeToInsert = new(id, 0, bounds);
            PerformInsertNode(nodeToInsert);
        }
        
        public void RemoveLeafNode(int id)
        {
            if (!TraverseDFS(Root, id, out SpatialNode<T> nodeToRemove))
            {
                Debug.Log("[ BVH ] Node to remove does not exist.");
                return;
            }
            PerformRemoveNode(nodeToRemove);
        }

        public void UpdateBounds(int id, T updatedBounds)
        {
            if (!TraverseDFS(Root, id, out SpatialNode<T> nodeToRemove))
            {
                Debug.Log("[ BVH ] Node to update does not exist.");
                return;
            }
            PerformRemoveNode(nodeToRemove);
            
            SpatialNode<T> nodeToInsert = new(nodeToRemove.Id, 0, updatedBounds);
            PerformInsertNode(nodeToInsert);
        }

        public float CalculateCost()
        {
            float cost = 0f;
            TraverseDFS(node =>
            {
                cost += node.Bounds.HalfArea();
            });
            return cost;
        }

#if UNITY_EDITOR
        public void DrawStructure()
        {
            if (Root == null) return;

            TraverseDFS(DrawBound);

            void DrawBound(SpatialNode<T> node)
            {
                if (node.IsLeaf)
                {
                    Gizmos.color = Color.white;
                    Gizmos.DrawWireCube(node.Bounds.Center(), node.Bounds.Extents());
                    Color leafCol = Color.white;
                    leafCol.a *= 0.1f;
                    Gizmos.color = leafCol;
                    Gizmos.DrawCube(node.Bounds.Center(), node.Bounds.Extents());
                }
                else
                {
                    float colorIntensity = 1f - (node.Height - 1f) / Root.Height;
                    Color color = Color.HSVToRGB(node.Height / 10f % 1, 1, 1);
                    Gizmos.color = color * colorIntensity;
                    Gizmos.DrawWireCube(node.Bounds.Center(), node.Bounds.Extents());
                }
            }
        }
#endif

        private void PerformInsertNode(SpatialNode<T> nodeToInsert)
        {
            if (Root == null)
            {
                Root = nodeToInsert;
                return;
            }
            
            T insertNodeBound = nodeToInsert.Bounds;
            SpatialNode<T> bestSibling = Root;
            float bestCost = Root.Bounds.Union(insertNodeBound).HalfArea();
            
            // Find best sibling
            Queue<(SpatialNode<T>, float)> searchQueue = new();
            searchQueue.Enqueue((Root, 0f));
            
            while (searchQueue.Count != 0)
            {
                (SpatialNode<T> currentNode, float inheritedCost) = searchQueue.Dequeue();
                float directCost = currentNode.Bounds.Union(insertNodeBound).HalfArea();
                float currentCost = directCost + inheritedCost;

                if (currentCost < bestCost)
                {
                    bestSibling = currentNode;
                    bestCost = currentCost;
                }

                inheritedCost += directCost - currentNode.Bounds.HalfArea();
                float lowerBoundCost = insertNodeBound.HalfArea() + inheritedCost;
                if (!(lowerBoundCost < bestCost) || currentNode.IsLeaf) continue;

                searchQueue.Enqueue((currentNode.LeftChild, inheritedCost));
                searchQueue.Enqueue((currentNode.RightChild, inheritedCost));
            }
            
            // Create new parent
            SpatialNode<T> oldParent = bestSibling.Parent;
            SpatialNode<T> newParent = new()
            {
                Id = SpatialNode<T>.InternalNodeId,
                Bounds = bestSibling.Bounds.Union(insertNodeBound),
                Height = bestSibling.Height + 1,
                Parent = oldParent,
                LeftChild = bestSibling,
                RightChild = nodeToInsert,
            };

            if (oldParent == null)
                Root = newParent;
            else
            {
                if (oldParent.LeftChild == bestSibling)
                    oldParent.LeftChild = newParent;
                else
                    oldParent.RightChild = newParent;
            }
            
            bestSibling.Parent = newParent;
            nodeToInsert.Parent = newParent;
            
            BalanceTree(nodeToInsert);
        }
        
        private void PerformRemoveNode(SpatialNode<T> nodeToRemove)
        {
            if (Root == nodeToRemove)
            {
                Root = null;
                return;
            }
            
            SpatialNode<T> parent = nodeToRemove.Parent;
            SpatialNode<T> sibling = parent.LeftChild == nodeToRemove ? parent.RightChild : parent.LeftChild;
            if (parent.Parent == null) // is root
            {
                Root = sibling;
                sibling.Parent = null;
                nodeToRemove.Parent = null;
            }
            else
            {
                sibling.Parent = parent.Parent;
                if (parent.Parent.LeftChild == parent)
                    parent.Parent.LeftChild = sibling;
                else
                    parent.Parent.RightChild = sibling;
                nodeToRemove.Parent = null;

                BalanceTree(sibling);
            }
        }

        private void BalanceTree(SpatialNode<T> node)
        {
            SpatialNode<T> ancestor = node.Parent;
            while (ancestor != null)
            {
                SpatialNode<T> rotated = TreeRotations(ancestor);
                rotated.UpdateBounds();
                rotated.UpdateHeight();
                ancestor = rotated.Parent;
                
                if (rotated.Height > MaxHeight)
                    MaxHeight = rotated.Height;
            }
        }
        
        private SpatialNode<T> TreeRotations(SpatialNode<T> node)
        {
            if (node == null || node.IsLeaf || node.Height < 2) return node;

            SpatialNode<T> leftChild = node.LeftChild;
            SpatialNode<T> rightChild = node.RightChild;

            int balanceFactor = node.GetBalanceFactor();
            if (balanceFactor > 1) // left heavy
                return Rotate(true, node, rightChild, leftChild.Bounds);
            if (balanceFactor < -1)
                return Rotate(false, node, leftChild, rightChild.Bounds);
            
            node.UpdateHeight();
            return node;
        }
        
        private SpatialNode<T> Rotate(bool rotateRight, SpatialNode<T> node, SpatialNode<T> rotateChild, T heavyBound)
        {
            SpatialNode<T> left = rotateChild.LeftChild;
            SpatialNode<T> right = rotateChild.RightChild;
            rotateChild.LeftChild = node;
            rotateChild.Parent = node.Parent;
            node.Parent = rotateChild;

            if (rotateChild.Parent == null)
                Root = rotateChild;
            else
            {
                if (rotateChild.Parent.LeftChild == node)
                    rotateChild.Parent.LeftChild = rotateChild;
                else
                    rotateChild.Parent.RightChild = rotateChild;
            }

            if (left.Height > right.Height)
                Swap(left, right);
            else
                Swap(right, left);
            
            node.UpdateHeight();
            rotateChild.UpdateHeight();
            return rotateChild;
            
            void Swap(SpatialNode<T> a, SpatialNode<T> b)
            {
                rotateChild.RightChild = a;
                if (rotateRight)
                    node.RightChild = b;
                else
                    node.LeftChild = b;
                b.Parent = node;
                node.Bounds = heavyBound.Union(b.Bounds);
                rotateChild.Bounds = node.Bounds.Union(a.Bounds);
            }
        }
        
        private void TraverseDFS(Action<SpatialNode<T>> action)
        {
            Stack<SpatialNode<T>> stack = new();
            stack.Push(Root);

            while (stack.Count != 0)
            {
                SpatialNode<T> current = stack.Pop();
                if (current == null) break;

                action(current);
                if (current.IsLeaf) continue;
                
                if (current.LeftChild != null) stack.Push(current.LeftChild);
                if (current.RightChild != null) stack.Push(current.RightChild);
            }
        }
        
        private bool TraverseDFS(SpatialNode<T> current, int id, out SpatialNode<T> targetNode)
        {
            if (current == null)
            {
                targetNode = null;
                return false;
            }
            if (current.Id != id)
            {
                return TraverseDFS(current.LeftChild, id, out targetNode) ||
                       TraverseDFS(current.RightChild, id, out targetNode);
            }
            targetNode = current;
            return true;
        }
        
        private void TraverseBFS(Action<SpatialNode<T>> action)
        {
            if (Root == null) return;

            Queue<SpatialNode<T>> queue = new();
            queue.Enqueue(Root);

            while (queue.Count > 0)
            {
                SpatialNode<T> current = queue.Dequeue();
                action(current);

                if (current.LeftChild != null) queue.Enqueue(current.LeftChild);
                if (current.RightChild != null) queue.Enqueue(current.RightChild);
            }
        }
    }
}

