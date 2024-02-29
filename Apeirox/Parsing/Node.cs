namespace Apeirox.Parsing;
public class Node
{
    public string Type { get; set; }
    public string? Value { get; set; }
    public List<Node> Children { get; set; } = new List<Node>();

    public Node(string type, string? value = null)
    { 
        Type = type;
        Value = value;
    }

    public string? GetValue()
    {
        if (Value == null && Children.Count == 1)
        {
            var node = Children[0];
            return node.GetValue();
        }

        return Value;
    }
}
