namespace Compiler.Lexing;

public class Token
{
    public string Name { get; set; }
    public string Value { get; set; }

    public Token(string name, string value = "")
    {
        Name = name;
        Value = value;
    }
}
