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
        lexerBuilder.Define("sign", @"(\+|-)");
        lexerBuilder.Define("scale", @"(\*|/)");
        lexerBuilder.Define("number", @"\d*\.?\d+");
        lexerBuilder.Define("string", @"""([^""]*)""");
        lexerBuilder.Define("identifier", @"[A-Za-z_]\w*");
        lexerBuilder.Define("=", @"=");
        lexerBuilder.Define("(", @"\(");
        lexerBuilder.Define(")", @"\)");
        lexerBuilder.Define("[", @"\[");
        lexerBuilder.Define("]", @"\]");
        lexerBuilder.Define(".", @"\.");
        lexerBuilder.Define(",", @",");
        lexerBuilder.Define("space", @"\s+");
        lexerBuilder.Ignore("space");

        var parserBuilder = new ParserBuilder(lexerBuilder.Build());
        parserBuilder.Define("Program", ["Statement"]); // id = 0
        parserBuilder.Define("Statement", ["Assignment"]); // id = 1
        parserBuilder.Define("Statement", ["Expression"]); // id = 2
        parserBuilder.Define("Assignment", ["identifier", "=", "Expression"]); // id = 3
        parserBuilder.Define("Assignment", ["ArrayAccess", "=", "Expression"]); // id = 4
        parserBuilder.Define("Assignment", ["PropertyAccess", "=", "Expression"]); // id = 5
        parserBuilder.Define("Expression", ["Expression", "sign", "Term"]); // id = 6
        parserBuilder.Define("Expression", ["Term"]); // id = 7
        parserBuilder.Define("Term", ["Term", "scale", "Factor"]); // id = 8
        parserBuilder.Define("Term", ["Factor"]); // id = 9
        parserBuilder.Define("Factor", ["(", "Expression", ")"]); // id = 10
        parserBuilder.Define("Factor", ["number"]); // id = 11
        parserBuilder.Define("Factor", ["string"]); // id = 12
        parserBuilder.Define("Factor", ["identifier"]); // id = 13
        parserBuilder.Define("Factor", ["Array"]); // id = 14
        parserBuilder.Define("Factor", ["ArrayAccess"]); // id = 15
        parserBuilder.Define("Factor", ["PropertyAccess"]); // id = 16
        parserBuilder.Define("Factor", ["MethodAccess"]); // id = 17
        parserBuilder.Define("Array", ["[", "List", "]"]); // id = 18
        parserBuilder.Define("List", ["Expression"]); // id = 19
        parserBuilder.Define("List", ["List", ",", "Expression"]); // id = 20
        parserBuilder.Define("ArrayAccess", ["identifier", "[", "Expression", "]"]); // id = 21
        parserBuilder.Define("PropertyAccess", ["identifier", ".", "identifier"]); // id = 22
        parserBuilder.Define("MethodAccess", ["identifier", ".", "identifier", "(", "List", ")"]); // id = 23
        parserBuilder.Define("MethodAccess", ["identifier", ".", "identifier", "(", ")"]); // id = 24

        parser = parserBuilder.Build();
    }

    [Fact]
    public void TestOperatorsOrderOfPrecedence()
    {
        var statement = "x = 2 + 3 * 4.5";
        parser.Consume(statement);

        var count = 0;
        while (parser.Status != ParserStatus.Accepted)
        {
            parser.Advance();
            if (parser.Status == ParserStatus.Reduced)
            {
                switch (parser.RuleId)
                {
                    case 3:
                        Assert.Equal("x", parser.GetValue(0));
                        Assert.Equal("=", parser.GetValue(1));
                        count += parser.RuleId;
                        break;
                    case 6:
                        Assert.Equal("2", parser.GetValue(0));
                        Assert.Equal("+", parser.GetValue(1));
                        count += parser.RuleId;
                        break;
                    case 8:
                        Assert.Equal("3", parser.GetValue(0));
                        Assert.Equal("*", parser.GetValue(1));
                        Assert.Equal("4.5", parser.GetValue(2));
                        count += parser.RuleId;
                        break;
                }
            }
        }

        // Ensure that all cases are handled.
        Assert.Equal(3 + 8 + 6, count);

        output.WriteLine("Parse tree:");
        var options = new JsonSerializerOptions { WriteIndented = true };
        var parseTree = JsonSerializer.Serialize(parser.Node, options);
        output.WriteLine(parseTree);
    }

    [Fact]
    public void TestParenthesesOverridePrecedence()
    {
        var statement = "x = (2 + 3) * 4.5";
        parser.Consume(statement);

        var count = 0;
        while (parser.Status != ParserStatus.Accepted)
        {
            parser.Advance();
            if (parser.Status == ParserStatus.Reduced)
            {
                switch (parser.RuleId)
                {
                    case 3:
                        Assert.Equal("x", parser.GetValue(0));
                        Assert.Equal("=", parser.GetValue(1));
                        count += parser.RuleId;
                        break;
                    case 10:
                        Assert.Equal("(", parser.GetValue(0));
                        Assert.Equal(")", parser.GetValue(2));
                        count += parser.RuleId;
                        break;
                    case 6:
                        Assert.Equal("2", parser.GetValue(0));
                        Assert.Equal("+", parser.GetValue(1));
                        Assert.Equal("3", parser.GetValue(2));
                        count += parser.RuleId;
                        break;
                    case 8:
                        Assert.Equal("*", parser.GetValue(1));
                        Assert.Equal("4.5", parser.GetValue(2));
                        count += parser.RuleId;
                        break;
                }
            }
        }

        // Ensure that all cases are handled.
        Assert.Equal(3 + 8 + 10 + 6, count);

        output.WriteLine("Parse tree:");
        var options = new JsonSerializerOptions { WriteIndented = true };
        var parseTree = JsonSerializer.Serialize(parser.Node, options);
        output.WriteLine(parseTree);
    }

    [Fact]
    public void TestAssigningArrayToArrayIndex()
    {
        var statement = "array[0] = [1, 2]";
        parser.Consume(statement);

        var count = 0;
        while (parser.Status != ParserStatus.Accepted)
        {
            parser.Advance();
            if (parser.Status == ParserStatus.Reduced)
            {
                switch (parser.RuleId)
                {
                    case 18:
                        Assert.Equal("[", parser.GetValue(0));
                        Assert.Equal("]", parser.GetValue(2));
                        count += parser.RuleId;
                        break;
                    case 20:
                        Assert.Equal("1", parser.GetValue(0));
                        Assert.Equal(",", parser.GetValue(1));
                        Assert.Equal("2", parser.GetValue(2));
                        count += parser.RuleId;
                        break;
                    case 21:
                        Assert.Equal("array", parser.GetValue(0));
                        Assert.Equal("[", parser.GetValue(1));
                        Assert.Equal("0", parser.GetValue(2));
                        Assert.Equal("]", parser.GetValue(3));
                        count += parser.RuleId;
                        break;
                }
            }
        }

        // Ensure that all cases are handled.
        Assert.Equal(18 + 20 + 21, count);

        output.WriteLine("Parse tree:");
        var options = new JsonSerializerOptions { WriteIndented = true };
        var parseTree = JsonSerializer.Serialize(parser.Node, options);
        output.WriteLine(parseTree);
    }

    [Fact]
    public void TestAssigningStringToProperty()
    {
        var statement = "person.Name = \"Alice\"";
        parser.Consume(statement);

        var count = 0;
        while (parser.Status != ParserStatus.Accepted)
        {
            parser.Advance();
            if (parser.Status == ParserStatus.Reduced)
            {
                switch (parser.RuleId)
                {
                    case 22:
                        Assert.Equal("person", parser.GetValue(0));
                        Assert.Equal(".", parser.GetValue(1));
                        Assert.Equal("Name", parser.GetValue(2));
                        count += parser.RuleId;
                        break;
                    case 5:
                        Assert.Equal("=", parser.GetValue(1));
                        Assert.Equal("\"Alice\"", parser.GetValue(2));
                        count += parser.RuleId;
                        break;
                }
            }
        }

        // Ensure that all cases are handled.
        Assert.Equal(22 + 5, count);

        output.WriteLine("Parse tree:");
        var options = new JsonSerializerOptions { WriteIndented = true };
        var parseTree = JsonSerializer.Serialize(parser.Node, options);
        output.WriteLine(parseTree);
    }

    [Fact]
    public void TestMethodCallSingleArgument()
    {
        var statement = "person.Call(911)";
        parser.Consume(statement);

        var count = 0;
        while (parser.Status != ParserStatus.Accepted)
        {
            parser.Advance();
            if (parser.Status == ParserStatus.Reduced)
            {
                switch (parser.RuleId)
                {
                    case 23:
                        Assert.Equal("person", parser.GetValue(0));
                        Assert.Equal(".", parser.GetValue(1));
                        Assert.Equal("Call", parser.GetValue(2));
                        Assert.Equal("(", parser.GetValue(3));
                        Assert.Equal("911", parser.GetValue(4));
                        Assert.Equal(")", parser.GetValue(5));
                        count += parser.RuleId;
                        break;
                }
            }
        }

        // Ensure that all cases are handled.
        Assert.Equal(23, count);

        output.WriteLine("Parse tree:");
        var options = new JsonSerializerOptions { WriteIndented = true };
        var parseTree = JsonSerializer.Serialize(parser.Node, options);
        output.WriteLine(parseTree);
    }

    [Fact]
    public void TestMethodCallWithArguments()
    {
        var statement = "collection.Add(1, 2)";
        parser.Consume(statement);

        var count = 0;
        while (parser.Status != ParserStatus.Accepted)
        {
            parser.Advance();
            if (parser.Status == ParserStatus.Reduced)
            {
                switch (parser.RuleId)
                {
                    case 23:
                        Assert.Equal("collection", parser.GetValue(0));
                        Assert.Equal(".", parser.GetValue(1));
                        Assert.Equal("Add", parser.GetValue(2));
                        Assert.Equal("(", parser.GetValue(3));
                        Assert.Equal(")", parser.GetValue(5));
                        count += parser.RuleId;
                        break;
                    case 20:
                        Assert.Equal("1", parser.GetValue(0));
                        Assert.Equal(",", parser.GetValue(1));
                        Assert.Equal("2", parser.GetValue(2));
                        count += parser.RuleId;
                        break;
                }
            }
        }

        // Ensure that all cases are handled.
        Assert.Equal(23 + 20, count);

        output.WriteLine("Parse tree:");
        var options = new JsonSerializerOptions { WriteIndented = true };
        var parseTree = JsonSerializer.Serialize(parser.Node, options);
        output.WriteLine(parseTree);
    }

    [Fact]
    public void TestMethodCallWithoutArguments()
    {
        var statement = "collection.Count()";
        parser.Consume(statement);

        var count = 0;
        while (parser.Status != ParserStatus.Accepted)
        {
            parser.Advance();
            if (parser.Status == ParserStatus.Reduced)
            {
                switch (parser.RuleId)
                {
                    case 24:
                        Assert.Equal("collection", parser.GetValue(0));
                        output.WriteLine(parser.GetValue(0));
                        Assert.Equal(".", parser.GetValue(1));
                        Assert.Equal("Count", parser.GetValue(2));
                        Assert.Equal("(", parser.GetValue(3));
                        Assert.Equal(")", parser.GetValue(4));
                        count += parser.RuleId;
                        break;
                }
            }
        }

        // Ensure that all cases are handled.
        Assert.Equal(24, count);

        output.WriteLine("Parse tree:");
        var options = new JsonSerializerOptions { WriteIndented = true };
        var parseTree = JsonSerializer.Serialize(parser.Node, options);
        output.WriteLine(parseTree);
    }
}
