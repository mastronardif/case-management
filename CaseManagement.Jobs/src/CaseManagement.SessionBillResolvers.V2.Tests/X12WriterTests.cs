using CaseManagement.SessionBillResolvers.V2.Engine.Dsl;
using CaseManagement.SessionBillResolvers.V2.Engine.Steps;

namespace CaseManagement.SessionBillResolvers.V2.Tests;

public class X12WriterTests
{
    [Fact]
    public void BuildsBasic837PEnvelope()
    {
        var writer = new X12Writer();
        var output = writer.Build("{" + "\"loops\": { \"1000A\": {} }" + "}");

        Assert.Contains("ISA", output);
        Assert.Contains("GS", output);
        Assert.Contains("ST", output);
        Assert.Contains("SE", output);
    }

    [Fact]
    public void ParserResolvesWTokenToX12Writer()
    {
        var registry = OperatorRegistry.FromJson("{\"tokens\": []}").MergeWith(OperatorRegistry.BuiltInTokens);
        var expr = PipelineParser.Parse("1220 (W)", registry);

        Assert.True(registry.TryResolve("W", out var token));
        Assert.Equal("x12Writer", token.Operator);
        Assert.IsType<ChainExpr>(expr);
    }

    [Fact]
    public void UsesDocumentIdForOutputFileName()
    {
        var fileName = X12WriterStep.BuildOutputFileName(1220);

        Assert.Equal("1220.edi.txt", fileName);
    }
}
