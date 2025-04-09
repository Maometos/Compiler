namespace Compiler.Parsing;

public class Rule
{
    public int Id { get; }
    public string Head { get; }
    public string[] Body { get; }

    public Rule(int id, string head, string[] body)
    {
        Id = id;
        Head = head;
        Body = body;
    }
}
