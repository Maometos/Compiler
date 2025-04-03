namespace Compiler.Lexing;

public class LexerBuilder
{
    private Dictionary<string, string> patterns = new Dictionary<string, string>();
    private List<string> ignoredPatterns = [];

    public void Define(string name, string pattern)
    {
        patterns.Add(name, pattern);
    }

    public void Ignore(string name)
    {
        ignoredPatterns.Add(name);
    }

    public Lexer Build()
    {
        return new Lexer(patterns, ignoredPatterns);
    }
}
