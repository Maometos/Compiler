using Apeirox.Lexing;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Xunit.Abstractions;

namespace Apeirox.Tests;

public class LexerTest
{
    [Fact]
    public void Test()
    {
        LexerBuilder lexerBuilder = new LexerBuilder();
        lexerBuilder.Take("variable", @"[A-Za-z_][\w]*");
        lexerBuilder.Take("number", @"\d");
        lexerBuilder.Take("equal", @"=");
        lexerBuilder.Take("semicolon", @";");
        lexerBuilder.Skip(@"\s+");

        var lexer = lexerBuilder.Build();
        var statment = "x1 = 5;";
        lexer.Consume(statment);

        lexer.Advance();
        Assert.Equal("x1", lexer.Token.Value);

        lexer.Advance();
        Assert.Equal("=", lexer.Token.Value);

        lexer.Advance();
        Assert.Equal("5", lexer.Token.Value);

        lexer.Advance();
        Assert.Equal(";", lexer.Token.Value);
    }

    [Fact]
    public void TestException()
    {
        LexerBuilder lexerBuilder = new LexerBuilder();
        lexerBuilder.Take("variable", @"[A-Za-z_][\w]*");

        var lexer = lexerBuilder.Build();
        var statment = "x1 = 5;";
        lexer.Consume(statment);
        //The first occurrence must match x1 according to the given pattern.
        lexer.Advance();
        Assert.Equal("x1", lexer.Token.Value);

        // This must throw an exception because there is no pattern match for the next occurrence.
        Assert.Throws<LexerException>(() => lexer.Advance());
    }
}