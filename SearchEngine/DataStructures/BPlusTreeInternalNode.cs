namespace SearchEngine.DataStructures;

public class BPlusTreeInternalNode<TKey, TValue> : BPlusTreeNode<TKey, TValue> where TKey : IComparable<TKey>
{
    public List<BPlusTreeNode<TKey, TValue>> Children { get; set; }
    public override bool IsLeaf => false;

    public BPlusTreeInternalNode(int order) : base(order)
    {
        Children = new List<BPlusTreeNode<TKey, TValue>>();
    }
}
