using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace CaseManagement.SessionBillResolvers.V2;

public enum BillingRunMode { Loop, SingleRun }

public record RunInput(
    string RunId,
    string RunAction,
    [property: JsonPropertyName("inputDoc")] int InputDoc,
    int ProjectionDefinitionId,
    int BillingRuleId,
    [property: JsonPropertyName("case-number")] int? CaseNumber,
    [property: JsonPropertyName("session-number")] int? SessionNumber,
    DateTime CreatedDate);

public record RunFile(
    [property: JsonPropertyName("sessionDoc")] RunInput? SessionDoc,
    [property: JsonPropertyName("projector")]  RunInput? Projector);

public record BillingRunOptions(BillingRunMode Mode, string? CaseNumber = null, int? SessionNumber = null, RunInput? RunInput = null);

public record DocumentContext(
    int? DocumentId   = null,
    int? CaseId       = null,
    int? WorkbookQId  = null,
    int? SessionId    = null,
    string? DocumentType = null);


public record WorkflowStep(
    string Id,
    string Operator,
    IReadOnlyList<JsonElement> Input,
    IReadOnlyList<string> Output);

public record WorkflowDefinition(
    string WorkflowId,
    string Version,
    IReadOnlyList<WorkflowStep> Steps,
    Dictionary<string, JsonElement>? Params = null);

public record WorkflowStepResult(
    string Id,
    string Operator,
    IReadOnlyList<string> InputTokens,
    IReadOnlyList<string> OutputNames,
    int[] ResolvedInputs,
    int[] ResolvedOutputs);

public record WorkflowRunManifest(
    string RunId,
    int WorkflowDocId,
    string WorkflowId,
    string Version,
    DateTime StartedAt,
    DateTime CompletedAt,
    IReadOnlyList<WorkflowStepResult> Steps);

public class BillingSettings
{
    public int BatchSize { get; set; } = 100;
    public string Mode { get; set; } = "Default";
}

public class SessionData
{
    public int SessionId { get; set; }
    public int CaseId { get; set; }
    public string CaseNumber { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
}

public class Invoice
{
    public int SessionId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public record Claim837PData(
    IReadOnlyList<JsonObject> Session,
    IReadOnlyList<JsonObject> Case,
    IReadOnlyList<JsonObject> Coverage,
    IReadOnlyList<JsonObject> Authorization,
    IReadOnlyList<JsonObject> Diagnoses,
    IReadOnlyList<JsonObject> Provider,
    IReadOnlyList<JsonObject> Assessment,
    JsonNode?                 Definition);

public record OperatorParam(string Name, string Type, bool Required, string Description);

public record OperatorInfo(
    string Operator,
    string Description,
    string[] InputLabels,
    string[] OutputLabels,
    OperatorParam[] Params);

public class BillingResult
{
    public int InvoiceCount { get; set; }
}

public class Document
{
    public int DocumentId { get; set; }
    public Guid? VersionId { get; set; }
    public int? CaseId { get; set; }
    public int? WorkbookQId { get; set; }
    public int? SessionId { get; set; }
    public string? DocumentType { get; set; }
    public string? Title { get; set; }
    public string? FileName { get; set; }
    public string? ContentType { get; set; }
    public byte[] FileData { get; set; } = [];
    public DateTime? CreatedDate { get; set; }
    public string? CreatedBy { get; set; }
    public bool IsActive { get; set; }

    public string Content => System.Text.Encoding.UTF8.GetString(FileData);
}