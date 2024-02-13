using Apeirox.Lexing;
using Xunit.Abstractions;

namespace Apeirox.Tests;

public class CompilerTest
{
    private readonly ITestOutputHelper output;
    public CompilerTest(ITestOutputHelper output)
    {
        this.output = output;
    }

    [Fact]
    public void Test()
    {
        LexerBuilder lexerBuilder = new LexerBuilder();
        lexerBuilder.Take("variable", @"[A-Za-z_][\w]*");
        lexerBuilder.Take("number", @"\d");
        lexerBuilder.Take("times", @"(\*|/)");
        lexerBuilder.Take("sign", @"(\+|-)");
        lexerBuilder.Take("equal", @"=");
        lexerBuilder.Take("semicolon", @";");
        lexerBuilder.Skip(@"\s+");

        var lexer = lexerBuilder.Build();
        var statment = "x1 = 2 * 5 + 4;";
        lexer.Consume(statment);

        do
        {
            lexer.Advance();
            output.WriteLine("Token: " + lexer.Token.Type + " => " + lexer.Token.Value);
        } while (lexer.Token.Type != "END");
    }
}