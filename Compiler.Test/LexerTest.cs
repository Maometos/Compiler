using Compiler.Lexing;
using Xunit.Abstractions;

namespace Compiler.Tests;

public class LexerTest
{
    private readonly ITestOutputHelper output;

    public LexerTest(ITestOutputHelper output)
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
        var statement = "x1 = 2 * 5 + 4;";
        lexer.Consume(statement);

        string[] expectedValues = ["x1", "=", "2", "*", "5", "+", "4", ";"];
        var index = 0;

        while (true)
        {
            lexer.Advance();

            if (lexer.Token.Type == "END")
            {
                break;
            }

            Assert.Equal(expectedValues[index], lexer.Token.Value);
            output.WriteLine("Token: " + lexer.Token.Type + " => " + lexer.Token.Value);
            index++;
        }
    }

    [Fact]
    public void TestException()
    {
        LexerBuilder lexerBuilder = new LexerBuilder();
        lexerBuilder.Take("variable", @"[A-Za-z_][\w]*");

        var lexer = lexerBuilder.Build();
        var statement = "x1 = 5;";
        lexer.Consume(statement);
        //The first occurrence must match x1 according to the given pattern.
        lexer.Advance();
        Assert.Equal("x1", lexer.Token.Value);

        // This must throw an exception because there is no pattern match for the next occurrence.
        Assert.Throws<LexerException>(() => lexer.Advance());
    }
}
