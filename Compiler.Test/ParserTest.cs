using Compiler.Lexing;
using Compiler.Parsing;
using System.Text.Json;
using Xunit.Abstractions;

namespace Compiler.Tests;

public class ParserTest
{
    private readonly ITestOutputHelper output;
    private Parser parser;

    public ParserTest(ITestOutputHelper output)
    {
        this.output = output;

        var lexerBuilder = new LexerBuilder();
        lexerBuilder.Define("=", @"=");
        lexerBuilder.Define("(", @"\(");
        lexerBuilder.Define(")", @"\)");
        lexerBuilder.Define("sign", @"(\+|-)");
        lexerBuilder.Define("times", @"(\*|/)");
        lexerBuilder.Define("number", @"\d");
        lexerBuilder.Define("identifier", @"[A-Za-z_][\w]*");
        lexerBuilder.Define("space", @"\s+");
        lexerBuilder.Ignore("space");

        var parserBuilder = new ParserBuilder(lexerBuilder.Build());
        parserBuilder.Define("Statement", ["Expression"]); // id = 0
        parserBuilder.Define("Expression", ["Expression", "sign", "Term"]); // id = 1
        parserBuilder.Define("Expression", ["Term"]); // id = 2
        parserBuilder.Define("Term", ["Term", "times", "Factor"]); // id = 3
        parserBuilder.Define("Term", ["Factor"]); // id = 4
        parserBuilder.Define("Factor", ["(", "Expression", ")"]); // id = 5
        parserBuilder.Define("Factor", ["number"]); // id = 6
        parserBuilder.Define("Factor", ["identifier"]); // id = 7
        parserBuilder.Define("Factor", ["Assignment"]); // id = 8
        parserBuilder.Define("Assignment", ["identifier", "=", "Expression"]); // id = 9

        parser = parserBuilder.Build();
    }

    [Fact]
    public void Test()
    {
        var statement = "x = 5 * (4 + 3)";
        parser.Consume(statement);

        while (parser.Status != ParserStatus.Accepted)
        {
            parser.Advance();
            if (parser.Status == ParserStatus.Reduced)
            {
                switch (parser.RuleId)
                {
                    case 1:
                        var leftOperand = parser.GetValue(0);
                        var operatorSymbol = parser.GetValue(1);
                        var rightOperand = parser.GetValue(2);
                        Assert.Equal("4", leftOperand);
                        Assert.Equal("+", operatorSymbol);
                        Assert.Equal("3", rightOperand);
                        break;
                    case 5:
                        var leftBracket = parser.GetValue(0);
                        var rightBracket = parser.GetValue(2);
                        Assert.Equal("(", leftBracket);
                        Assert.Equal(")", rightBracket);
                        break;
                    case 9:
                        var identifier = parser.GetValue(0);
                        var equality = parser.GetValue(1);
                        Assert.Equal("x", identifier);
                        Assert.Equal("=", equality);
                        break;
                }
            }
        }

        output.WriteLine("Parse tree:");
        var options = new JsonSerializerOptions { WriteIndented = true };
        var parseTree = JsonSerializer.Serialize(parser.Node, options);
        output.WriteLine(parseTree);
    }
}
