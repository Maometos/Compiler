using Compiler.Lexing;
using Compiler.Parsing;
using System.Text.Json;
using Xunit.Abstractions;

namespace Compiler.Tests;

public class ParserTest
{
    private readonly ITestOutputHelper output;

    public ParserTest(ITestOutputHelper output)
    {
        this.output = output;
    }

    [Fact]
    public void Test()
    {
        var lexerBuilder = new LexerBuilder();

        lexerBuilder.Define("number", @"\d");
        lexerBuilder.Define("times", @"(\*|/)");
        lexerBuilder.Define("sign", @"(\+|-)");
        lexerBuilder.Define("(", @"\(");
        lexerBuilder.Define(")", @"\)");
        lexerBuilder.Define("=", @"=");
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

        var statement = "5 * (4 + 3)";
        var parser = parserBuilder.Build();

        parser.Consume(statement);

        while (parser.Status != ParserStatus.Accepted)
        {
            parser.Advance();
            if (parser.Status == ParserStatus.Reduced)
            {
                if (parser.ReduceId == 5)
                {
                    var leftValue = parser.GetValue(0);
                    var rightValue = parser.GetValue(2);
                    Assert.Equal("(", leftValue);
                    Assert.Equal(")", rightValue);
                    output.WriteLine("Reduced rule Id: 5");
                    output.WriteLine("left token value: " + leftValue);
                    output.WriteLine("right token value: " + rightValue);
                }
            }
        }

        output.WriteLine("Parse tree:");
        var options = new JsonSerializerOptions { WriteIndented = true };
        var parseTree = JsonSerializer.Serialize(parser.Node, options);
        output.WriteLine(parseTree);
    }
}
