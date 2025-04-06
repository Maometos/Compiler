namespace Compiler.Parsing;

public class Node
{
    public string Name { get; set; }
    public string? Value { get; set; }
    public List<Node> Children { get; set; } = [];

    public Node(string type, string? value = null)
    {
        Name = type;
        Value = value;
    }
}
