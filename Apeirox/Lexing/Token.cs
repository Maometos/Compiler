namespace Apeirox.Lexing;
public class Token
{
    public string Type { get; set; }
    public string Value { get; set; }

    public Token(string name, string value = "")
    {
        Type = name;
        Value = value;
    }
}
