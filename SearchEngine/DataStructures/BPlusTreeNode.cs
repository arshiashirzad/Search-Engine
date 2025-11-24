namespace SearchEngine.DataStructures;

public abstract class BPlusTreeNode<TKey, TValue> where TKey : IComparable<TKey>
{
    public List<TKey> Keys { get; protected set; }
    public int MaxKeys { get; protected set; }
    public abstract bool IsLeaf { get; }

    protected BPlusTreeNode(int order)
    {
        MaxKeys = order - 1;
        Keys = new List<TKey>();
    }

    public bool IsFull => Keys.Count >= MaxKeys;
    public bool NeedsSplit => Keys.Count > MaxKeys;
}
