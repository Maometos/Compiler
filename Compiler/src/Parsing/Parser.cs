using Compiler.Lexing;

namespace Compiler.Parsing;

public class Parser
{
    private Stack<int[]> stateStack = new Stack<int[]>();
    private Stack<Node> nodeStack = new Stack<Node>();
    private Queue<Token> bucket = new Queue<Token>();
    private List<Rule> rules;
    private Lexer lexer;

    public ParserStatus Status { get; set; } = ParserStatus.Shifted;
    public Node Node => nodeStack.Count > 0 ? nodeStack.Peek() : new Node("");
    public int? RuleId { get; set; } = null;

    public Parser(List<Rule> rules, Lexer lexer)
    {
        this.rules = rules;
        this.lexer = lexer;
    }

    public void Consume(string input)
    {
        lexer.Consume(input);
        // Temporary shifting of the initial token so that can be used for retrieving the initial matched rule.
        lexer.Advance();
        var initialToken = lexer.Token;
        var initialRule = GetMatchedRule(rules, initialToken.Name);
        if (initialRule != null)
        {
            // Initial state.
            stateStack.Push([initialRule.Id, -1]);
        }

        // The parser's bucket could have tokens stored during the retrieving of the initial matched rule process because of the lookahead method.
        // The initial token must be moved back to the front of the parser's bucket so that it can be processed when the first shift action is performed.
        var bucket = new Queue<Token>();
        bucket.Enqueue(initialToken);
        foreach (var token in this.bucket)
        {
            bucket.Enqueue(token);
        }

        this.bucket = bucket;
    }

    private Rule? GetMatchedRule(List<Rule> rules, string item, int position = 0)
    {
        var matchedRules = new List<Rule>();
        foreach (var rule in rules)
        {
            if (rule.Body.Length > position && rule.Body[position] == item)
            {
                matchedRules.Add(rule);
            }
        }

        if (matchedRules.Count == 0) return null;
        if (matchedRules.Count == 1) return matchedRules.First();

        var token = Lookahead(position);
        var matchedRule = GetMatchedRule(matchedRules, token.Name, position + 1);
        if (matchedRule != null) return matchedRule;
        return matchedRules[0];
    }

    private Token Lookahead(int depth = 0)
    {
        var index = 0;
        foreach (var token in bucket)
        {
            if (index == depth)
            {
                return token;
            }

            index++;
        }

        for (int i = 0; i <= depth - bucket.Count(); i++)
        {
            lexer.Advance();
            bucket.Enqueue(lexer.Token);
        }

        return lexer.Token;
    }

    public void Advance()
    {
        if (Status == ParserStatus.Accepted)
        {
            return;
        }

        if (Reduce())
        {
            var node = nodeStack.Peek();
            if (stateStack.Count == 0)
            {
                Goto(node.Name);
                return;
            }

            var state = stateStack.Peek();
            var rule = rules[state[0]];
            var position = state[1];

            var nextToken = Lookahead();
            // Do nothing if the next token can be shifted for the current rule in the next action.
            if (rule.Body.Length > position + 1 && rule.Body[position + 1] == nextToken.Name)
            {
                return;
            }

            // Do nothing if can be reduced for the current rule in the next action.
            if (rule.Body.Length == position + 1 && rule.Body[position] == node.Name)
            {
                if (nextToken.Name == "END")
                {
                    return;
                }

                var nextRule = GetMatchedRule(rules, nextToken.Name, position);
                // Do nothing if the current item doesn't has a follow that need to be shifted in the next action
                if (nextRule != null && nextRule.Body[0] != node.Name)
                {
                    return;
                }
            }

            Goto(node.Name);
        }
        else
        {
            Shift();
            var state = stateStack.Peek();
            var rule = rules[state[0]];
            var position = state[1];
            var node = nodeStack.Peek();

            if (rule.Body.Length > position && rule.Body[position] != node.Name)
            {
                Goto(node.Name);
            }
        }
    }

    private bool Reduce()
    {
        if (stateStack.Count == 0 || nodeStack.Count == 0) return false;

        var node = nodeStack.Peek();
        var state = stateStack.Peek();
        var rule = rules[state[0]];
        var position = state[1];

        // Return false when the current pointer is not at the end position or the current item does not match the item of the current rule at the given position.
        if (rule.Body.Length != position + 1 || rule.Body[position] != node.Name) return false;

        var nextToken = Lookahead();
        var nextRule = GetMatchedRule(rules, nextToken.Name, position);
        // Return false when the current item has a follow that need to be shifted in the next action
        if (nextRule != null && nextRule.Body[0] == node.Name) return false;

        node = new Node(rule.Head);
        for (int i = 0; i <= position; i++)
        {
            var childNode = nodeStack.Pop();
            node.Children.Add(childNode);
        }

        node.Children.Reverse();
        nodeStack.Push(node);
        stateStack.Pop();
        RuleId = rule.Id;

        Status = ParserStatus.Reduced;
        if (rule.Id == 0)
        {
            Status = ParserStatus.Accepted;
        }

        return true;
    }

    private void Shift()
    {
        Token token;
        if (bucket.Count > 0)
        {
            token = bucket.Dequeue();
        }
        else
        {
            lexer.Advance();
            token = lexer.Token;
        }

        nodeStack.Push(new Node(token.Name, token.Value));

        var state = stateStack.Pop();
        var position = state[1];
        state[1] = position + 1;
        stateStack.Push(state);

        RuleId = state[0];
        Status = ParserStatus.Shifted;
    }

    private void Goto(string item)
    {
        var rule = GetMatchedRule(rules, item);
        if (rule != null)
        {
            int[] state = [rule.Id, 0];
            stateStack.Push(state);
        }

        //throw new ParserException("No matched rule for the item: '" + item);
    }

    public string? GetValue(int? index = null)
    {
        if (index == null)
        {
            return Node.Value;
        }

        if (Node.Children.Count() <= index)
        {
            return null;
        }

        var node = Node.Children[(int)index];
        while (node.Children.Count == 1)
        {
            node = node.Children[0];
        }

        return node.Value;
    }
}
