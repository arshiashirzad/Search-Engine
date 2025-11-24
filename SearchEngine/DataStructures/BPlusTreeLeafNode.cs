namespace SearchEngine.DataStructures;

public class BPlusTreeLeafNode<TKey, TValue> : BPlusTreeNode<TKey, TValue> where TKey : IComparable<TKey>
{
    public List<TValue> Values { get; set; }
    public BPlusTreeLeafNode<TKey, TValue>? Next { get; set; }
    public override bool IsLeaf => true;

    public BPlusTreeLeafNode(int order) : base(order)
    {
        Values = new List<TValue>();
        Next = null;
    }
}
