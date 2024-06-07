namespace Compiler.Parsing;

public class Rule
{
    public int Id { get; }
    public string Name { get; }
    public string[] Predicate { get; }

    public Rule(int id, string name, string[] predicate)
    {
        Id = id;
        Name = name;
        Predicate = predicate;
    }
}
