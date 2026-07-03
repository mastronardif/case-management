using System.Text.Json;

namespace CaseManagement.SessionBillResolvers.V2.Engine.Dsl;

public static class PipelineCompiler
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static WorkflowDefinition Compile(PipelineExpr expr, string workflowId = "dsl-pipeline")
    {
        var steps = expr switch
        {
            StandaloneExpr s  => CompileStandalone(s),
            BundleExpr     b  => CompileBundle(b),
            ChainExpr      c  => CompileChain(c),
            ComparisonExpr cm => CompileComparison(cm),
            _                 => throw new InvalidOperationException($"Unsupported expression type: {expr.GetType().Name}")
        };

        return new WorkflowDefinition(
            WorkflowId: workflowId,
            Version:    "1.0",
            Steps:      steps,
            Params:     null);
    }

    // (C)  — no doc inputs, reads entirely from wfParams
    private static IReadOnlyList<WorkflowStep> CompileStandalone(StandaloneExpr expr) =>
    [
        MakeStep("step1", expr.Operator, [], ["output"])
    ];

    // (Z)[a, b, c]  — N-ary, all items are inputs
    private static IReadOnlyList<WorkflowStep> CompileBundle(BundleExpr expr) =>
    [
        MakeStep("step1", expr.Operator, expr.Items.Select(AtomToken).ToArray(), ["output"])
    ];

    // $S (P) $Proj (R) $Rule  — binary chain; unary step has only left input
    private static IReadOnlyList<WorkflowStep> CompileChain(ChainExpr expr)
    {
        var steps = new List<WorkflowStep>();

        for (int i = 0; i < expr.Steps.Count; i++)
        {
            var chainStep = expr.Steps[i];
            var left      = i == 0 ? AtomToken(expr.First) : StepRefToken(i); // D1, D2...
            var inputs    = chainStep.Operand is null
                ? new[] { left }
                : new[] { left, AtomToken(chainStep.Operand) };

            steps.Add(MakeStep($"step{i + 1}", chainStep.Operator, inputs, [$"D{i + 1}"]));
        }

        return steps;
    }

    // $S (P) $Proj == $Rule  — chain then projectorComparer on result vs right-hand side
    private static IReadOnlyList<WorkflowStep> CompileComparison(ComparisonExpr expr)
    {
        // Build the chain steps first
        var chainSteps = CompileChain(new ChainExpr(expr.First, expr.Steps)).ToList();
        var chainCount = chainSteps.Count;

        // Final comparison step: projectorComparer(lastOutput, right)
        var lastOutputRef = chainCount == 0
            ? AtomToken(expr.First)
            : StepRefToken(chainCount);

        chainSteps.Add(MakeStep(
            $"step{chainCount + 1}",
            "projectorComparer",
            [lastOutputRef, AtomToken(expr.Right)],
            ["comparison.json", "review.html"]));

        return chainSteps;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static WorkflowStep MakeStep(string id, string op, JsonElement[] inputs, string[] outputs) =>
        new(id, op, inputs, outputs);

    // Atom → JsonElement token (matching WorkflowEngine.ResolveInput format)
    private static JsonElement AtomToken(PipelineAtom atom) => atom switch
    {
        ParamAtom  p => JsonSerializer.SerializeToElement($"${p.Name}"),   // "$sessionDocId"
        StepRefAtom s => JsonSerializer.SerializeToElement($"D{s.Index}"), // "D1"
        LiteralAtom l => JsonSerializer.SerializeToElement(l.DocId),       // 206  (bare number)
        _             => throw new InvalidOperationException($"Unknown atom type: {atom.GetType().Name}")
    };

    // Implicit step-output reference: D{stepNumber} where stepNumber is 1-based
    private static JsonElement StepRefToken(int stepNumber) =>
        JsonSerializer.SerializeToElement($"D{stepNumber}");
}
