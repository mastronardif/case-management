namespace CaseManagement.SessionBillResolvers.V2.Engine.Dsl;

// ── Tokens ────────────────────────────────────────────────────────────────────

abstract record DslToken;
record OperatorTok(string Token)   : DslToken;  // (P) (R) (Z) etc.
record ParamTok(string Name)       : DslToken;  // $sessionDocId
record StepRefTok(int Index)       : DslToken;  // D1 D2
record LiteralTok(int DocId)       : DslToken;  // 206
record BracketOpenTok              : DslToken;  // [
record BracketCloseTok             : DslToken;  // ]
record CommaTok                    : DslToken;  // ,
record CompareOpTok                : DslToken;  // ==

// ── AST ───────────────────────────────────────────────────────────────────────

public abstract record PipelineAtom;
public record ParamAtom(string Name)      : PipelineAtom;  // $name
public record StepRefAtom(int Index)      : PipelineAtom;  // D1
public record LiteralAtom(int DocId)      : PipelineAtom;  // 206

public abstract record PipelineExpr;

// $S (P) $Proj (R) $Rule  — binary chain, right operand may be null for unary
public record ChainStep(string Operator, PipelineAtom? Operand);
public record ChainExpr(PipelineAtom First, IReadOnlyList<ChainStep> Steps)           : PipelineExpr;

// (Z)[$D1, $D2, 206]  — N-ary bundle
public record BundleExpr(string Operator, IReadOnlyList<PipelineAtom> Items)          : PipelineExpr;

// (C)  — standalone, no doc inputs
public record StandaloneExpr(string Operator)                                          : PipelineExpr;

// $S (P) $Proj == $Rule  — chain followed by comparison
public record ComparisonExpr(PipelineAtom First, IReadOnlyList<ChainStep> Steps, PipelineAtom Right) : PipelineExpr;

// ── Parser ────────────────────────────────────────────────────────────────────

public static class PipelineParser
{
    public static PipelineExpr Parse(string expression, OperatorRegistry registry)
    {
        var tokens = Tokenize(expression);
        var pos    = 0;

        DslToken? Peek() => pos < tokens.Count ? tokens[pos] : null;
        DslToken  Consume() => tokens[pos++];

        bool IsAtom(DslToken? t) => t is ParamTok or StepRefTok or LiteralTok;

        PipelineAtom ParseAtom()
        {
            var t = Consume();
            return t switch
            {
                ParamTok p   => new ParamAtom(p.Name),
                StepRefTok s => new StepRefAtom(s.Index),
                LiteralTok l => new LiteralAtom(l.DocId),
                _            => throw new InvalidOperationException($"Expected atom but got {t.GetType().Name} in '{expression}'")
            };
        }

        PipelineExpr ParseExpr()
        {
            // Bundle or standalone: expression starts with (OP)
            if (Peek() is OperatorTok leadOp)
            {
                if (!registry.TryResolve(leadOp.Token, out var leadDef))
                    throw new InvalidOperationException($"Unknown operator token '{leadOp.Token}' in '{expression}'");

                Consume(); // consume (OP)

                if (Peek() is BracketOpenTok)
                {
                    // Bundle: (Z)[atom, atom, ...]
                    Consume(); // [
                    var items = new List<PipelineAtom> { ParseAtom() };
                    while (Peek() is CommaTok) { Consume(); items.Add(ParseAtom()); }
                    if (Peek() is not BracketCloseTok)
                        throw new InvalidOperationException($"Expected ']' in '{expression}'");
                    Consume(); // ]
                    return new BundleExpr(leadDef.Operator, items);
                }

                // Standalone: (C) — no inputs
                return new StandaloneExpr(leadDef.Operator);
            }

            // Chain or unary: starts with an atom
            var first = ParseAtom();
            var steps = new List<ChainStep>();

            while (Peek() is OperatorTok opTok)
            {
                if (!registry.TryResolve(opTok.Token, out var opDef))
                    throw new InvalidOperationException($"Unknown operator token '{opTok.Token}' in '{expression}'");

                Consume(); // consume (OP)

                // Binary if next token is an atom, unary otherwise
                if (IsAtom(Peek()))
                    steps.Add(new ChainStep(opDef.Operator, ParseAtom()));
                else
                    steps.Add(new ChainStep(opDef.Operator, null));
            }

            // Comparison: chain == atom
            if (Peek() is CompareOpTok)
            {
                Consume(); // ==
                return new ComparisonExpr(first, steps, ParseAtom());
            }

            return new ChainExpr(first, steps);
        }

        var result = ParseExpr();
        if (pos < tokens.Count)
            throw new InvalidOperationException($"Unexpected token at position {pos}: '{tokens[pos].GetType().Name}' in '{expression}'");
        return result;
    }

    // ── Tokenizer ─────────────────────────────────────────────────────────────

    internal static IReadOnlyList<DslToken> Tokenize(string input)
    {
        var tokens = new List<DslToken>();
        var i      = 0;

        while (i < input.Length)
        {
            if (char.IsWhiteSpace(input[i])) { i++; continue; }

            // Operator: (TOKEN)
            if (input[i] == '(')
            {
                i++;
                var start = i;
                while (i < input.Length && input[i] != ')') i++;
                tokens.Add(new OperatorTok(input[start..i].Trim()));
                i++; // skip )
                continue;
            }

            // Param: $name
            if (input[i] == '$')
            {
                i++;
                var start = i;
                while (i < input.Length && (char.IsLetterOrDigit(input[i]) || input[i] == '_')) i++;
                tokens.Add(new ParamTok(input[start..i]));
                continue;
            }

            // Step ref: D followed immediately by digits
            if (input[i] == 'D' && i + 1 < input.Length && char.IsDigit(input[i + 1]))
            {
                i++;
                var start = i;
                while (i < input.Length && char.IsDigit(input[i])) i++;
                tokens.Add(new StepRefTok(int.Parse(input[start..i])));
                continue;
            }

            // Literal int docId
            if (char.IsDigit(input[i]))
            {
                var start = i;
                while (i < input.Length && char.IsDigit(input[i])) i++;
                tokens.Add(new LiteralTok(int.Parse(input[start..i])));
                continue;
            }

            // Punctuation
            if (input[i] == '[') { tokens.Add(new BracketOpenTok());  i++; continue; }
            if (input[i] == ']') { tokens.Add(new BracketCloseTok()); i++; continue; }
            if (input[i] == ',') { tokens.Add(new CommaTok());         i++; continue; }

            // Compare ==
            if (input[i] == '=' && i + 1 < input.Length && input[i + 1] == '=')
            {
                tokens.Add(new CompareOpTok());
                i += 2;
                continue;
            }

            throw new InvalidOperationException($"Unexpected character '{input[i]}' at position {i} in '{input}'");
        }

        return tokens;
    }
}
