namespace Apeirox.Lexing;

public class LexerBuilder
{
    private Dictionary<string, string> patterns = new Dictionary<string, string>();

    public void Take(string name, string pattern)
    {
        patterns.Add(name, pattern);
    }

    public void Skip(string pattern)
    {
        patterns.Add("SKIP", pattern);
    }

    public Lexer Build()
    {
        return new Lexer(patterns);
    }
}
