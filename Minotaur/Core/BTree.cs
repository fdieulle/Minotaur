using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Minotaur.Core
{
    /// <summary>
    /// Based on BTree chapter in "Introduction to Algorithms", by Thomas Cormen, Charles Leiserson, Ronald Rivest.
    /// 
    /// This implementation is not thread-safe, and user must handle thread-safety.
    /// </summary>
    /// <typeparam name="TKey">Type of BTree Key.</typeparam>
    /// <typeparam name="TValue">Type of BTree Value associated with each Key.</typeparam>
    public class BTree<TKey, TValue> : IEnumerable<Entry<TKey, TValue>>
        where TKey : IComparable<TKey>
    {
        public BTree(int degree)
        {
            if (degree < 2)
                throw new ArgumentException("BTree degree must be at least 2", nameof(degree));

            _root = new Node(degree);
            Degree = degree;
        }

        private Node _root;

        public int Degree { get; }

        public int Height { get; private set; } = 1;

        /// <summary>
        /// Searches a key in the BTree, returning the entry with it and with the pointer.
        /// </summary>
        /// <param name="key">Key being searched.</param>
        /// <returns>Entry for that key, null otherwise.</returns>
        public Entry<TKey, TValue> Search(TKey key) => SearchInternal(_root, key);

        public IEnumerable<Entry<TKey, TValue>> Search(TKey start, TKey end)
        {
            return SearchInternal(_root, start, end);
        }

        /// <summary>
        /// Inserts a new key associated with a pointer in the BTree. This
        /// operation splits nodes as required to keep the BTree properties.
        /// </summary>
        /// <param name="newKey">Key to be inserted.</param>
        /// <param name="newPointer">Value to be associated with inserted key.</param>
        public void Insert(TKey newKey, TValue newPointer)
        {
            // there is space in the root node
            if (!_root.HasReachedMaxEntries)
            {
                InsertNonFull(_root, newKey, newPointer);
                return;
            }

            // need to create new node and have it split
            var oldRoot = _root;
            _root = new Node(Degree);
            _root.Children.Add(oldRoot);
            SplitChild(_root, 0, oldRoot);
            InsertNonFull(_root, newKey, newPointer);

            Height++;
        }

        /// <summary>
        /// Deletes a key from the BTree. This operations moves keys and nodes
        /// as required to keep the BTree properties.
        /// </summary>
        /// <param name="keyToDelete">Key to be deleted.</param>
        public void Delete(TKey keyToDelete)
        {
            DeleteInternal(_root, keyToDelete);

            // if root's last entry was moved to a child node, remove it
            if (_root.Entries.Count != 0 || _root.IsLeaf) return;

            _root = _root.Children.Single();
            Height--;
        }

        /// <summary>
        /// Internal method to delete keys from the BTree
        /// </summary>
        /// <param name="node">Node to use to start search for the key.</param>
        /// <param name="keyToDelete">Key to be deleted.</param>
        private void DeleteInternal(Node node, TKey keyToDelete)
        {
            var i = node.Entries.TakeWhile(entry => keyToDelete.CompareTo(entry.Key) > 0).Count();

            // found key in node, so delete if from it
            if (i < node.Entries.Count && node.Entries[i].Key.CompareTo(keyToDelete) == 0)
            {
                DeleteKeyFromNode(node, keyToDelete, i);
                return;
            }

            // delete key from subtree
            if (!node.IsLeaf)
            {
                DeleteKeyFromSubtree(node, keyToDelete, i);
            }
        }

        /// <summary>
        /// Helper method that deletes a key from a subtree.
        /// </summary>
        /// <param name="parentNode">Parent node used to start search for the key.</param>
        /// <param name="keyToDelete">Key to be deleted.</param>
        /// <param name="subtreeIndexInNode">Index of subtree node in the parent node.</param>
        private void DeleteKeyFromSubtree(Node parentNode, TKey keyToDelete, int subtreeIndexInNode)
        {
            var childNode = parentNode.Children[subtreeIndexInNode];

            // node has reached min # of entries, and removing any from it will break the btree property,
            // so this block makes sure that the "child" has at least "degree" # of nodes by moving an 
            // entry from a sibling node or merging nodes
            if (childNode.HasReachedMinEntries)
            {
                var leftIndex = subtreeIndexInNode - 1;
                var leftSibling = subtreeIndexInNode > 0 ? parentNode.Children[leftIndex] : null;

                var rightIndex = subtreeIndexInNode + 1;
                var rightSibling = subtreeIndexInNode < parentNode.Children.Count - 1
                                                ? parentNode.Children[rightIndex]
                                                : null;

                if (leftSibling != null && leftSibling.Entries.Count > Degree - 1)
                {
                    // left sibling has a node to spare, so this moves one node from left sibling 
                    // into parent's node and one node from parent into this current node ("child")
                    childNode.Entries.Insert(0, parentNode.Entries[subtreeIndexInNode]);
                    parentNode.Entries[subtreeIndexInNode] = leftSibling.Entries.Last();
                    leftSibling.Entries.RemoveAt(leftSibling.Entries.Count - 1);

                    if (!leftSibling.IsLeaf)
                    {
                        childNode.Children.Insert(0, leftSibling.Children.Last());
                        leftSibling.Children.RemoveAt(leftSibling.Children.Count - 1);
                    }
                }
                else if (rightSibling != null && rightSibling.Entries.Count > Degree - 1)
                {
                    // right sibling has a node to spare, so this moves one node from right sibling 
                    // into parent's node and one node from parent into this current node ("child")
                    childNode.Entries.Add(parentNode.Entries[subtreeIndexInNode]);
                    parentNode.Entries[subtreeIndexInNode] = rightSibling.Entries.First();
                    rightSibling.Entries.RemoveAt(0);

                    if (!rightSibling.IsLeaf)
                    {
                        childNode.Children.Add(rightSibling.Children.First());
                        rightSibling.Children.RemoveAt(0);
                    }
                }
                else
                {
                    // this block merges either left or right sibling into the current node "child"
                    if (leftSibling != null)
                    {
                        childNode.Entries.Insert(0, parentNode.Entries[subtreeIndexInNode]);
                        var oldEntries = childNode.Entries;
                        childNode.Entries = leftSibling.Entries;
                        childNode.Entries.AddRange(oldEntries);
                        if (!leftSibling.IsLeaf)
                        {
                            var oldChildren = childNode.Children;
                            childNode.Children = leftSibling.Children;
                            childNode.Children.AddRange(oldChildren);
                        }

                        parentNode.Children.RemoveAt(leftIndex);
                        parentNode.Entries.RemoveAt(subtreeIndexInNode);
                    }
                    else
                    {
                        Debug.Assert(rightSibling != null, "Node should have at least one sibling");
                        childNode.Entries.Add(parentNode.Entries[subtreeIndexInNode]);
                        childNode.Entries.AddRange(rightSibling.Entries);
                        if (!rightSibling.IsLeaf)
                        {
                            childNode.Children.AddRange(rightSibling.Children);
                        }

                        parentNode.Children.RemoveAt(rightIndex);
                        parentNode.Entries.RemoveAt(subtreeIndexInNode);
                    }
                }
            }

            // at this point, we know that "child" has at least "degree" nodes, so we can
            // move on - this guarantees that if any node needs to be removed from it to
            // guarantee BTree's property, we will be fine with that
            DeleteInternal(childNode, keyToDelete);
        }

        /// <summary>
        /// Helper method that deletes key from a node that contains it, be this
        /// node a leaf node or an internal node.
        /// </summary>
        /// <param name="node">Node that contains the key.</param>
        /// <param name="keyToDelete">Key to be deleted.</param>
        /// <param name="keyIndexInNode">Index of key within the node.</param>
        private void DeleteKeyFromNode(Node node, TKey keyToDelete, int keyIndexInNode)
        {
            // if leaf, just remove it from the list of entries (we're guaranteed to have
            // at least "degree" # of entries, to BTree property is maintained
            if (node.IsLeaf)
            {
                node.Entries.RemoveAt(keyIndexInNode);
                return;
            }

            var predecessorChild = node.Children[keyIndexInNode];
            if (predecessorChild.Entries.Count >= Degree)
            {
                var predecessor = DeletePredecessor(predecessorChild);
                node.Entries[keyIndexInNode] = predecessor;
            }
            else
            {
                var successorChild = node.Children[keyIndexInNode + 1];
                if (successorChild.Entries.Count >= Degree)
                {
                    var successor = DeleteSuccessor(predecessorChild);
                    node.Entries[keyIndexInNode] = successor;
                }
                else
                {
                    predecessorChild.Entries.Add(node.Entries[keyIndexInNode]);
                    predecessorChild.Entries.AddRange(successorChild.Entries);
                    predecessorChild.Children.AddRange(successorChild.Children);

                    node.Entries.RemoveAt(keyIndexInNode);
                    node.Children.RemoveAt(keyIndexInNode + 1);

                    DeleteInternal(predecessorChild, keyToDelete);
                }
            }
        }

        /// <summary>
        /// Helper method that deletes a predecessor key (i.e. rightmost key) for a given node.
        /// </summary>
        /// <param name="node">Node for which the predecessor will be deleted.</param>
        /// <returns>Predecessor entry that got deleted.</returns>
        private static Entry<TKey, TValue> DeletePredecessor(Node node)
        {
            if (node.IsLeaf)
            {
                var result = node.Entries[node.Entries.Count - 1];
                node.Entries.RemoveAt(node.Entries.Count - 1);
                return result;
            }

            return DeletePredecessor(node.Children.Last());
        }

        /// <summary>
        /// Helper method that deletes a successor key (i.e. leftmost key) for a given node.
        /// </summary>
        /// <param name="node">Node for which the successor will be deleted.</param>
        /// <returns>Successor entry that got deleted.</returns>
        private static Entry<TKey, TValue> DeleteSuccessor(Node node)
        {
            if (node.IsLeaf)
            {
                var result = node.Entries[0];
                node.Entries.RemoveAt(0);
                return result;
            }

            return DeletePredecessor(node.Children.First());
        }

        /// <summary>
        /// Helper method that search for a key in a given BTree.
        /// </summary>
        /// <param name="node">Node used to start the search.</param>
        /// <param name="key">Key to be searched.</param>
        /// <returns>Entry object with key information if found, null otherwise.</returns>
        private static Entry<TKey, TValue> SearchInternal(Node node, TKey key)
        {
            while (true)
            {
                var i = 0;
                for (; i < node.Entries.Count && key.CompareTo(node.Entries[i].Key) > 0; i++)
                {
                }

                if (i < node.Entries.Count && node.Entries[i].Key.CompareTo(key) == 0)
                    return node.Entries[i];

                if (node.IsLeaf) return null;
                node = node.Children[i];
            }
        }

        /// <summary>
        /// Helper method that search for a range keys all intersect entries
        /// </summary>
        /// <param name="node">Node used to start the search.</param>
        /// <param name="start">Start key to be searched.</param>
        /// <param name="end">End key to be searched.</param>
        private static IEnumerable<Entry<TKey, TValue>> SearchInternal(Node node, TKey start, TKey end)
        {
            while (true)
            {
                var i = 0;
                for (; i < node.Entries.Count; i++)
                {
                    if (start.CompareTo(node.Entries[i].Key) > 0) continue;
                    if (end.CompareTo(node.Entries[i].Key) < 0) break;

                    if (i < node.Children.Count)
                        foreach (var entry in SearchInternal(node.Children[i], start, end))
                            yield return entry;

                    yield return node.Entries[i];
                }

                if (i < node.Children.Count)
                {
                    node = node.Children[i];
                    continue;
                }

                break;
            }
        }

        /// <summary>
        /// Helper method that splits a full node into two nodes.
        /// </summary>
        /// <param name="parentNode">Parent node that contains node to be split.</param>
        /// <param name="nodeToBeSplitIndex">Index of the node to be split within parent.</param>
        /// <param name="nodeToBeSplit">Node to be split.</param>
        private void SplitChild(Node parentNode, int nodeToBeSplitIndex, Node nodeToBeSplit)
        {
            var newNode = new Node(Degree);

            parentNode.Entries.Insert(nodeToBeSplitIndex, nodeToBeSplit.Entries[Degree - 1]);
            parentNode.Children.Insert(nodeToBeSplitIndex + 1, newNode);

            newNode.Entries.AddRange(nodeToBeSplit.Entries.GetRange(Degree, Degree - 1));

            // remove also Entries[this.Degree - 1], which is the one to move up to the parent
            nodeToBeSplit.Entries.RemoveRange(Degree - 1, Degree);

            if (!nodeToBeSplit.IsLeaf)
            {
                newNode.Children.AddRange(nodeToBeSplit.Children.GetRange(Degree, Degree));
                nodeToBeSplit.Children.RemoveRange(Degree, Degree);
            }
        }

        private void InsertNonFull(Node node, TKey newKey, TValue newPointer)
        {
            var positionToInsert = node.Entries.TakeWhile(entry => newKey.CompareTo(entry.Key) >= 0).Count();

            // leaf node
            if (node.IsLeaf)
            {
                node.Entries.Insert(positionToInsert, new Entry<TKey, TValue> { Key = newKey, Value = newPointer });
                return;
            }

            // non-leaf
            var child = node.Children[positionToInsert];
            if (child.HasReachedMaxEntries)
            {
                SplitChild(node, positionToInsert, child);
                if (newKey.CompareTo(node.Entries[positionToInsert].Key) > 0)
                {
                    positionToInsert++;
                }
            }

            InsertNonFull(node.Children[positionToInsert], newKey, newPointer);
        }

        private class Node
        {
            private readonly int _degree;

            public Node(int degree)
            {
                _degree = degree;
                Children = new List<Node>(degree);
                Entries = new List<Entry<TKey, TValue>>(degree);
            }

            public List<Node> Children { get; set; }

            public List<Entry<TKey, TValue>> Entries { get; set; }

            public bool IsLeaf => Children.Count == 0;

            public bool HasReachedMaxEntries => Entries.Count == 2 * _degree - 1;

            public bool HasReachedMinEntries => Entries.Count == _degree - 1;

            private TKey StartKey => IsLeaf ? Entries[0].Key : Children[0].StartKey;
            private TKey EndKey => IsLeaf ? Entries[Entries.Count - 1].Key : Children[Children.Count - 1].EndKey;

            public override string ToString()
            {
                return $"{StartKey} - {EndKey}";
            }
        }

        #region Implementation of IEnumerable

        public IEnumerator<Entry<TKey, TValue>> GetEnumerator()
        {
            foreach (var entry in Enumerate(_root))
                yield return entry;
        }

        private static IEnumerable<Entry<TKey, TValue>> Enumerate(Node node)
        {
            while(true)
            {
                var i = 0;
                for (; i < node.Entries.Count; i++)
                {
                    if (i < node.Children.Count)
                        foreach (var entry in Enumerate(node.Children[i]))
                            yield return entry;

                    yield return node.Entries[i];
                }

                if (i < node.Children.Count)
                {
                    node = node.Children[i];
                    continue;
                }

                break;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }

    public class Entry<TKey, TValue> : IEquatable<Entry<TKey, TValue>>
    {
        public TKey Key { get; set; }

        public TValue Value { get; set; }

        public bool Equals(Entry<TKey, TValue> other)
        {
            return other != null && Key.Equals(other.Key) && Value.Equals(other.Value);
        }

        public override string ToString()
        {
            return $"[{Key}] {Value}";
        }
    }
}
