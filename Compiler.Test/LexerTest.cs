using Compiler.Lexing;
using Xunit.Abstractions;

namespace Compiler.Tests;

public class LexerTest
{
    private readonly ITestOutputHelper output;
    private readonly Lexer lexer;

    public LexerTest(ITestOutputHelper output)
    {
        this.output = output;

        LexerBuilder lexerBuilder = new LexerBuilder();
        lexerBuilder.Define("variable", @"[A-Za-z_][\w]*");
        lexerBuilder.Define("number", @"\d");
        lexerBuilder.Define("times", @"(\*|/)");
        lexerBuilder.Define("sign", @"(\+|-)");
        lexerBuilder.Define("equal", @"=");
        lexerBuilder.Define("semicolon", @";");
        lexerBuilder.Define("space", @"\s+");
        lexerBuilder.Ignore("space");

        lexer = lexerBuilder.Build();
    }

    [Fact]
    public void Test()
    {
        var statement = "x1 = 2 * 5 + 4;";
        lexer.Consume(statement);

        var index = 0;
        string[] expectedValues = ["x1", "=", "2", "*", "5", "+", "4", ";"];

        while (true)
        {
            lexer.Advance();

            if (lexer.Token.Name == "END")
            {
                break;
            }

            Assert.Equal(expectedValues[index], lexer.Token.Value);
            output.WriteLine("Token: " + lexer.Token.Name + " => " + lexer.Token.Value);
            index++;
        }
    }

    [Fact]
    public void TestException()
    {
        var statement = "x1 = ?";
        lexer.Consume(statement);

        lexer.Advance();
        Assert.Equal("x1", lexer.Token.Value);

        lexer.Advance();
        Assert.Equal("=", lexer.Token.Value);

        // This must throw an exception because there is no registered pattern match for the character '?'
        Assert.Throws<LexerException>(() => lexer.Advance());
    }
}
