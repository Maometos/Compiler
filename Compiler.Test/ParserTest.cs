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
        lexerBuilder.Define("variable", @"[A-Za-z_][\w]*");
        lexerBuilder.Define("times", @"(\*|/)");
        lexerBuilder.Define("sign", @"(\+|-)");
        lexerBuilder.Define("(", @"\(");
        lexerBuilder.Define(")", @"\)");
        lexerBuilder.Define("=", @"=");
        lexerBuilder.Define("space", @"\s+");
        lexerBuilder.Ignore("space");

        var lexer = lexerBuilder.Build();
        var parserBuilder = new ParserBuilder(lexer);

        parserBuilder.Define("Statement", ["Expression"]); // id = 0
        parserBuilder.Define("Expression", ["Expression", "sign", "Term"]); // id = 1
        parserBuilder.Define("Expression", ["Term"]); // id = 2
        parserBuilder.Define("Term", ["Term", "times", "Factor"]); // id = 3
        parserBuilder.Define("Term", ["Factor"]); // id = 4
        parserBuilder.Define("Factor", ["(", "Expression", ")"]); // id = 5
        parserBuilder.Define("Factor", ["number"]); // id = 6
        parserBuilder.Define("Factor", ["variable"]); // id = 7
        parserBuilder.Define("Factor", ["Assignment"]); // id = 8
        parserBuilder.Define("Assignment", ["variable", "=", "Expression"]); // id = 9

        var parser = parserBuilder.Build();
        var statement = "x = 5 * (4 + 3)";
        parser.Consume(statement);

        while (parser.Status != ParserStatus.Accepted)
        {
            parser.Advance();
            if (parser.Status == ParserStatus.Reduced)
            {
                if (parser.ReduceId == 5)
                {
                    var leftNode = parser.Node.Children[0];
                    var rightNode = parser.Node.Children[2];
                    Assert.Equal("(", leftNode.GetValue());
                    Assert.Equal(")", rightNode.GetValue());
                    output.WriteLine("left node: " + leftNode.GetValue());
                    output.WriteLine(" right node: " + rightNode.GetValue());
                }
            }
        }

        output.WriteLine("Parse tree:");
        var options = new JsonSerializerOptions { WriteIndented = true };
        var parseTree = JsonSerializer.Serialize(parser.Node, options);
        output.WriteLine(parseTree);
    }
}
