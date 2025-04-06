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
            if (rule.Predicate.Length >= position + 1 && rule.Predicate[position] == item)
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

        if (!Reduce())
        {
            Shift();

        }

        Goto();
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

    private bool Reduce()
    {
        if (stateStack.Count == 0 || nodeStack.Count == 0) return false;

        var node = nodeStack.Peek();
        var state = stateStack.Peek();
        var rule = rules[state[0]];
        var position = state[1];

        if (rule.Predicate.Length != position + 1) return false;
        if (rule.Predicate[position] != node.Type) return false;
        var token = Lookahead();
        var nextRule = GetMatchedRule(rules, token.Name, position);
        // Check if the current item has a follow
        if (nextRule != null && nextRule.Predicate[0] == node.Type) return false;

        node = new Node(rule.Name);
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

    private void Goto()
    {
        if (Status == ParserStatus.Reduced)
        {
            Token token;
            Rule? rule;
            int[] state;
            var node = nodeStack.Peek();
            if (stateStack.Count == 0)
            {
                rule = GetMatchedRule(rules, node.Type);
                if (rule != null)
                {
                    state = [rule.Id, 0];
                    stateStack.Push(state);
                    return;
                }
            }

            state = stateStack.Peek();
            rule = rules[state[0]];
            var position = state[1];

            token = Lookahead();
            // Check if can be shifted
            if (rule.Predicate.Length > position + 1 && rule.Predicate[position] == node.Type && rule.Predicate[position + 1] == token.Name)
            {
                return;
            }

            // Checks if can be reduced
            if (rule.Predicate.Length == position + 1 && rule.Predicate[position] == node.Type)
            {
                token = Lookahead();
                if (token.Name == "END")
                {
                    return;
                }

                rule = GetMatchedRule(rules, token.Name, position);
                // Check if the current item has a follow
                if (rule != null && rule.Predicate[0] != node.Type)
                {
                    return;
                }
            }

            rule = GetMatchedRule(rules, node.Type);
            if (rule != null)
            {
                state = [rule.Id, 0];
                stateStack.Push(state);
            }
        }
        else if (Status == ParserStatus.Shifted)
        {
            var state = stateStack.Peek();
            var rule = rules[state[0]];
            var position = state[1];
            var node = nodeStack.Peek();

            if (rule.Predicate.Length >= position + 1 && rule.Predicate[position] != node.Type)
            {
                rule = GetMatchedRule(rules, node.Type);
                if (rule == null) throw new ParserException("No matched rule for the item: '" + node.Type + "' in the position: " + position);

                state = [rule.Id, 0];
                stateStack.Push(state);
            }
        }
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
