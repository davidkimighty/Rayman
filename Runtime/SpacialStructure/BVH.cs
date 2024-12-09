using System;
using System.Collections.Generic;
using System.Threading.Tasks;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Rayman
{
    public class BVH<T> : ISpatialStructure<T> where T : struct, IBounds<T>
    {
        private const int InternalNodeId = -1;
        
        public SpatialNode<T> Root { get; private set; }
        public int Count { get; private set; } = 0;
        
        public void Build(BoundingVolume<T>[] boundsToBuild)
        {
            for (int i = 0; i < boundsToBuild.Length; i++)
                CreateLeafNode(i, boundsToBuild[i].Bounds);
        }

        public async Task BuildAsync(BoundingVolume<T>[] boundsToBuild)
        {
            for (int i = 0; i < boundsToBuild.Length; i++)
            {
                CreateLeafNode(i, boundsToBuild[i].Bounds);
                await Task.Yield();
            }
        }

        public void UpdateBounds(int index, T updatedBounds)
        {
            if (!TraverseDFS(Root, index, out SpatialNode<T> oldNode))
            {
                Debug.Log("[ BVH ] Node to update does not exist.");
                return;
            }
            RemoveLeafNode(oldNode);
            CreateLeafNode(index, updatedBounds);
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

            HashSet<SpatialNode<T>> map = new();
            TraverseDFS(node =>
            {
                DrawBound(node);
                DrawBound(node.Parent);
                DrawBound(node.LeftChild);
                DrawBound(node.RightChild);
            });
            return;

            void DrawBound(SpatialNode<T> node)
            {
                if (node == null || map.Contains(node)) return;

                GUIStyle style = new GUIStyle();
                string nodeLabel;
                if (node.IsLeaf)
                {
                    style.normal.textColor = Handles.color = Color.green;
                    Handles.DrawWireCube(node.Bounds.Center(), node.Bounds.Extents());
                    nodeLabel = $"Node {node.Id} [ Leaf ]\n";
                }
                else
                {
                    float colorIntensity = 1f - (node.Height - 1f) / Root.Height;
                    Handles.color = Color.white * colorIntensity;
                    Handles.DrawWireCube(node.Bounds.Center(), node.Bounds.Extents());
                    style.normal.textColor = Color.white;
                    nodeLabel = $"Node {(node == Root ? $"[ Root ]" : $"[ Internal ]")}\n";
                }
                nodeLabel += $"Height: {node.Height}";
                Handles.Label(node.Bounds.Center(), nodeLabel, style);
                map.Add(node);
            }
        }
#endif

        private void CreateLeafNode(int id, T bounds)
        {
            SpatialNode<T> node = new(id, bounds, 0);
            InsertLeafNode(node);
            Count++;
        }

        private void InsertLeafNode(SpatialNode<T> nodeToInsert)
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
                Id = InternalNodeId,
                Bounds = bestSibling.Bounds.Union(insertNodeBound),
                Parent = oldParent,
                LeftChild = bestSibling,
                RightChild = nodeToInsert,
                Height = bestSibling.Height + 1,
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
            Count++;
            
            // Balance the tree
            SpatialNode<T> ancestor = nodeToInsert.Parent;
            while (ancestor != null)
            {
                SpatialNode<T> rotated = TreeRotations(ancestor);
                rotated.UpdateBounds();
                rotated.UpdateHeight();
                ancestor = rotated.Parent;
            }
        }

        private void RemoveLeafNode(SpatialNode<T> nodeToRemove)
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
                
                SpatialNode<T> ancestor = sibling.Parent;
                while (ancestor != null)
                {
                    SpatialNode<T> rotated = TreeRotations(ancestor);
                    rotated.UpdateBounds();
                    rotated.UpdateHeight();
                    ancestor = rotated.Parent;
                }
            }
            Count--;
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
        
        private bool TraverseDFS(SpatialNode<T> current, int targetId, out SpatialNode<T> targetNode)
        {
            if (current == null)
            {
                targetNode = null;
                return false;
            }
            if (current.Id != targetId)
            {
                return TraverseDFS(current.LeftChild, targetId, out targetNode) ||
                       TraverseDFS(current.RightChild, targetId, out targetNode);
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

