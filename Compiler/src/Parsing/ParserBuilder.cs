using Compiler.Lexing;

namespace Compiler.Parsing;

public class ParserBuilder
{
    public List<Rule> rules = new List<Rule>();
    private Lexer lexer;

    public ParserBuilder(Lexer lexer)
    {
        this.lexer = lexer;
    }

    public int Define(string head, string[] body)
    {
        var index = rules.Count;
        rules.Add(new Rule(index, head, body));
        return index;
    }

    public Parser Build()
    {
        return new Parser(rules, lexer);
    }
}
