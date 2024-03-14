using Apeirox.Lexing;
using Apeirox.Parsing;
using System.Text.Json;
using Xunit.Abstractions;

namespace Apeirox.Tests;

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
        lexerBuilder.Take("number", @"\d");
        lexerBuilder.Take("times", @"(\*|/)");
        lexerBuilder.Take("sign", @"(\+|-)");
        lexerBuilder.Take("(", @"\(");
        lexerBuilder.Take(")", @"\)");
        lexerBuilder.Skip(@"\s+");

        var lexer = lexerBuilder.Build();
        var parserBuilder = new ParserBuilder(lexer);

        int rule0 = parserBuilder.Define("Statement", ["Expression"]); // id = 0
        int rule1 = parserBuilder.Define("Expression", ["Expression", "sign", "Term"]); // id = 1
        int rule2 = parserBuilder.Define("Expression", ["Term"]); // id = 2
        int rule3 = parserBuilder.Define("Term", ["Term", "times", "Factor"]); // id = 3
        int rule4 = parserBuilder.Define("Term", ["Factor"]); // id = 4
        int rule5 = parserBuilder.Define("Factor", ["(", "Expression", ")"]); // id = 5
        int rule6 = parserBuilder.Define("Factor", ["number"]); // id = 6

        var parser = parserBuilder.Build();
        var statement = "5 + 4 * 3";
        parser.Consume(statement);

        while (parser.Status != ParserStatus.Accept)
        {
            parser.Advance();
            if (parser.Status == ParserStatus.Reduce)
            {
                if (parser.ReduceId == rule3)
                {
                    var leftNode = parser.Node.Children[0];
                    var rightNode = parser.Node.Children[2];
                    Assert.Equal("4", leftNode.GetValue());
                    Assert.Equal("3", rightNode.GetValue());
                    output.WriteLine("left: "+ leftNode.GetValue() + " right: " + rightNode.GetValue());
                }
            }
        }

        output.WriteLine("Parse tree:");
        var options = new JsonSerializerOptions { WriteIndented = true };
        var parseTree = JsonSerializer.Serialize(parser.Node, options);
        output.WriteLine(parseTree);
    }
}
