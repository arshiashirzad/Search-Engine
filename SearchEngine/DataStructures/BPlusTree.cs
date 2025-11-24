namespace SearchEngine.DataStructures;

public class BPlusTree<TKey, TValue> where TKey : IComparable<TKey>
{
    private BPlusTreeNode<TKey, TValue> _root;
    private readonly int _order;

    public BPlusTree(int order = 4)
    {
        _order = order;
        _root = new BPlusTreeLeafNode<TKey, TValue>(order);
    }

    public void Insert(TKey key, TValue value)
    {
        if (_root.NeedsSplit)
        {
            var newRoot = new BPlusTreeInternalNode<TKey, TValue>(_order);
            newRoot.Children.Add(_root);
            SplitChild(newRoot, 0);
            _root = newRoot;
        }

        InsertNonFull(_root, key, value);
    }

    private void InsertNonFull(BPlusTreeNode<TKey, TValue> node, TKey key, TValue value)
    {
        if (node.IsLeaf)
        {
            var leaf = (BPlusTreeLeafNode<TKey, TValue>)node;
            int pos = leaf.Keys.Count - 1;

            while (pos >= 0 && key.CompareTo(leaf.Keys[pos]) < 0)
            {
                pos--;
            }

            leaf.Keys.Insert(pos + 1, key);
            leaf.Values.Insert(pos + 1, value);
        }
        else
        {
            var internalNode = (BPlusTreeInternalNode<TKey, TValue>)node;
            int pos = internalNode.Keys.Count - 1;

            while (pos >= 0 && key.CompareTo(internalNode.Keys[pos]) < 0)
            {
                pos--;
            }
            pos++;

            if (internalNode.Children[pos].NeedsSplit)
            {
                SplitChild(internalNode, pos);
                if (key.CompareTo(internalNode.Keys[pos]) > 0)
                {
                    pos++;
                }
            }

            InsertNonFull(internalNode.Children[pos], key, value);
        }
    }

    private void SplitChild(BPlusTreeInternalNode<TKey, TValue> parent, int index)
    {
        var nodeToSplit = parent.Children[index];
        int midIndex = nodeToSplit.Keys.Count / 2;

        if (nodeToSplit.IsLeaf)
        {
            var leaf = (BPlusTreeLeafNode<TKey, TValue>)nodeToSplit;
            var newLeaf = new BPlusTreeLeafNode<TKey, TValue>(_order);

            for (int i = midIndex; i < leaf.Keys.Count; i++)
            {
                newLeaf.Keys.Add(leaf.Keys[i]);
                newLeaf.Values.Add(leaf.Values[i]);
            }

            leaf.Keys.RemoveRange(midIndex, leaf.Keys.Count - midIndex);
            leaf.Values.RemoveRange(midIndex, leaf.Values.Count - midIndex);

            newLeaf.Next = leaf.Next;
            leaf.Next = newLeaf;

            parent.Keys.Insert(index, newLeaf.Keys[0]);
            parent.Children.Insert(index + 1, newLeaf);
        }
        else
        {
            var internalNode = (BPlusTreeInternalNode<TKey, TValue>)nodeToSplit;
            var newInternal = new BPlusTreeInternalNode<TKey, TValue>(_order);

            TKey middleKey = internalNode.Keys[midIndex];

            for (int i = midIndex + 1; i < internalNode.Keys.Count; i++)
            {
                newInternal.Keys.Add(internalNode.Keys[i]);
            }

            for (int i = midIndex + 1; i < internalNode.Children.Count; i++)
            {
                newInternal.Children.Add(internalNode.Children[i]);
            }

            internalNode.Keys.RemoveRange(midIndex, internalNode.Keys.Count - midIndex);
            internalNode.Children.RemoveRange(midIndex + 1, internalNode.Children.Count - midIndex - 1);

            parent.Keys.Insert(index, middleKey);
            parent.Children.Insert(index + 1, newInternal);
        }
    }

    public TValue? Search(TKey key)
    {
        return SearchInNode(_root, key);
    }

    private TValue? SearchInNode(BPlusTreeNode<TKey, TValue> node, TKey key)
    {
        if (node.IsLeaf)
        {
            var leaf = (BPlusTreeLeafNode<TKey, TValue>)node;
            for (int i = 0; i < leaf.Keys.Count; i++)
            {
                if (leaf.Keys[i].CompareTo(key) == 0)
                {
                    return leaf.Values[i];
                }
            }
            return default;
        }
        else
        {
            var internalNode = (BPlusTreeInternalNode<TKey, TValue>)node;
            int pos = 0;

            while (pos < internalNode.Keys.Count && key.CompareTo(internalNode.Keys[pos]) >= 0)
            {
                pos++;
            }

            return SearchInNode(internalNode.Children[pos], key);
        }
    }

    public List<TValue> SearchRange(TKey startKey, TKey endKey)
    {
        var results = new List<TValue>();
        var leaf = FindLeafNode(_root, startKey);

        if (leaf == null) return results;

        bool collecting = false;

        while (leaf != null)
        {
            for (int i = 0; i < leaf.Keys.Count; i++)
            {
                if (leaf.Keys[i].CompareTo(startKey) >= 0 && leaf.Keys[i].CompareTo(endKey) <= 0)
                {
                    results.Add(leaf.Values[i]);
                    collecting = true;
                }
                else if (collecting && leaf.Keys[i].CompareTo(endKey) > 0)
                {
                    return results;
                }
            }
            leaf = leaf.Next;
        }

        return results;
    }

    private BPlusTreeLeafNode<TKey, TValue>? FindLeafNode(BPlusTreeNode<TKey, TValue> node, TKey key)
    {
        if (node.IsLeaf)
        {
            return (BPlusTreeLeafNode<TKey, TValue>)node;
        }

        var internalNode = (BPlusTreeInternalNode<TKey, TValue>)node;
        int pos = 0;

        while (pos < internalNode.Keys.Count && key.CompareTo(internalNode.Keys[pos]) >= 0)
        {
            pos++;
        }

        return FindLeafNode(internalNode.Children[pos], key);
    }

    public List<TValue> GetAllValues()
    {
        var results = new List<TValue>();
        var leaf = FindFirstLeaf(_root);

        while (leaf != null)
        {
            results.AddRange(leaf.Values);
            leaf = leaf.Next;
        }

        return results;
    }

    private BPlusTreeLeafNode<TKey, TValue> FindFirstLeaf(BPlusTreeNode<TKey, TValue> node)
    {
        if (node.IsLeaf)
        {
            return (BPlusTreeLeafNode<TKey, TValue>)node;
        }

        var internalNode = (BPlusTreeInternalNode<TKey, TValue>)node;
        return FindFirstLeaf(internalNode.Children[0]);
    }
}
