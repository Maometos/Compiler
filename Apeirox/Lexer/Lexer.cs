using System.Text;
using System.Text.RegularExpressions;

namespace Apeirox.Lexer;
public class Lexer
{
    private Dictionary<string, string> patterns;
    private StringBuilder input = new StringBuilder();
    private int offset = 0;
    public Token Token { get; set; } = new Token("START");

    public Lexer(Dictionary<string, string> patterns)
    {
        this.patterns = patterns;
    }

    public void Consume(string input)
    {
        this.input.Append(input);
        Token = new Token("START");
    }

    public void Advance()
    {
        bool matched = false;
        foreach (var pattern in patterns) {
            if (input.ToString() == string.Empty) {
                Token = new Token("END");
                break;
            } else
            {
                var match = Regex.Match(input.ToString(), "^" + pattern.Value);
                if (match.Success)
                {
                    matched = true;
                    var tokenValue = match.Value;
                    Token = new Token(pattern.Key, tokenValue);
                    offset += tokenValue.Length;
                    input.Remove(0, tokenValue.Length);
                   
                    if (pattern.Key == "SKIP")
                    {
                        Advance();
                    }
                    break;
                }
            }
        }

        if (!matched && input.ToString() != string.Empty) {
            throw new LexerException("Unmatched token for the input string at the offset of: " + offset);
        }
    }
}
