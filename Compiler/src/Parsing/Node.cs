namespace Compiler.Parsing;

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
}
