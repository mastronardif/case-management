using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;

namespace CaseManagement.SessionBillResolvers.V2.Jobs;

public record ClaimQueueSpec(int CaseId, int[] Sessions, int? Authorization);

public record ClearingHouseResult(
    int ClaimId, string ClaimNumber, int RuleDocId, int SourcesDocId,
    int EdiDocId, int AvailityReviewDocId, string Status, IReadOnlyList<string> Issues);

// In-process replacement for BuildQueueForClearingHouse.ps1 (Queue-triggered mode only — the
// -SpecFile mode isn't translated here since every real invocation of the PS1 script observed
// in practice used -QueueClaimId). Same sequence, same business rules, same doc-store/table
// writes — just real method calls into the same engine/repository instead of a PowerShell
// script shelling out to `dotnet run` and scraping doc ids out of console text.
public class ClearingHouseQueueJob(ClaimDataGateway data, DslRunner dsl, ILogger<ClearingHouseQueueJob> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    // No catalog entry exists for a blank starting invoice yet — fixed constant, same as the
    // PS1 script's $BlankInvoiceDocId.
    private const int BlankInvoiceDocId = 749;

    public async Task<ClearingHouseResult> RunAsync(int queueClaimId, CancellationToken ct)
    {
        var queueRow = await data.GetQueueClaimAsync(queueClaimId, ct)
            ?? throw new InvalidOperationException($"Queue1 row {queueClaimId} not found, or has no SpecDocumentId.");

        var specDoc = await dsl.GetDocumentAsync(queueRow.SpecDocumentId!.Value, ct);
        var spec = JsonSerializer.Deserialize<ClaimQueueSpec>(specDoc.Content, JsonOptions)
            ?? throw new InvalidOperationException($"Could not parse spec doc {queueRow.SpecDocumentId}.");

        var caseId = spec.CaseId;
        var invoice = BlankInvoiceDocId;

        logger.LogInformation("=== Build Queue For Clearing House === CaseId={CaseId} QueueClaimId={QueueClaimId}", caseId, queueClaimId);

        // Claim — created first; fails fast if any session is already claimed.
        logger.LogInformation("Claim: creating for sessions [{Sessions}]...", string.Join(", ", spec.Sessions));
        var claim = await data.CreateClaimAsync(caseId, spec.Sessions, ct);
        logger.LogInformation("  => claimId: {ClaimId} claimNumber: {ClaimNumber}", claim.ClaimId, claim.ClaimNumber);

        var claimDocId = await dsl.SaveSnapshotAsync(JsonSerializer.Serialize(new { claimNumber = claim.ClaimNumber }), "claim", ct);
        var claimProjectorDocId = await RequireProjectionAsync("Claim837P", ct);
        invoice = await dsl.RunSingleAsync($"{claimDocId} (P) {claimProjectorDocId} (AM) {invoice}", caseId, ct);

        // Sessions — each appends a Loop 2400 entry.
        if (spec.Sessions.Length > 0)
        {
            var sessionProjectorDocId = await RequireProjectionAsync("Session837P", ct);
            foreach (var sessionDocId in spec.Sessions)
                invoice = await dsl.RunSingleAsync($"{sessionDocId} (P) {sessionProjectorDocId} (AM) {invoice}", caseId, ct);
        }

        // Rendering Provider (loop 2310B) — resolved from the first claimed session's own
        // clinicianUsername. Billing provider (1000A/2010AA) comes entirely from Practice
        // Configuration.
        int? renderingProviderId = null;
        if (spec.Sessions.Length > 0)
        {
            logger.LogInformation("Rendering Provider: loading for session {SessionDocId}...", spec.Sessions[0]);
            var renderingProvider = await ResolveRenderingProviderAsync(spec.Sessions[0], ct);
            if (renderingProvider is not null)
            {
                renderingProviderId = renderingProvider.ProviderId;
                var renderingProviderDocId = await dsl.SaveSnapshotAsync(JsonSerializer.Serialize(new
                {
                    providerId = renderingProvider.ProviderId,
                    firstName = renderingProvider.FirstName,
                    lastName = renderingProvider.LastName,
                    npi = renderingProvider.NPI,
                    taxonomyCode = renderingProvider.TaxonomyCode,
                }), "rendering-provider", ct);
                var renderingProviderProjectorDocId = await RequireProjectionAsync("RenderingProvider837P", ct);
                invoice = await dsl.RunSingleAsync($"{renderingProviderDocId} (P) {renderingProviderProjectorDocId} (AM) {invoice}", caseId, ct);
            }
            else
            {
                logger.LogWarning("  No clinicianUsername on session {SessionDocId}, or no matching active Provider row.", spec.Sessions[0]);
            }
        }

        // Authorization.
        if (spec.Authorization is not null)
        {
            var authorizationProjectorDocId = await RequireProjectionAsync("Authorization837P", ct);
            invoice = await dsl.RunSingleAsync($"{spec.Authorization} (P) {authorizationProjectorDocId} (AM) {invoice}", caseId, ct);
        }

        // Insurance Coverage — drives SBR (2000B), Payer name (2010BB.NM1), and Subscriber
        // (2010BA). Resolved before Patient because RelationshipCode decides whether 2010CA
        // gets populated.
        logger.LogInformation("Insurance Coverage: loading for case {CaseId}...", caseId);
        var insuranceCoverage = await data.GetInsuranceCoverageAsync(caseId, ct);
        int? payerId = null;
        if (insuranceCoverage is not null)
        {
            payerId = insuranceCoverage.PayerId;
            var insuranceCoverageDocId = await dsl.SaveSnapshotAsync(JsonSerializer.Serialize(new
            {
                insuranceCoverageId = insuranceCoverage.InsuranceCoverageId,
                payerId = insuranceCoverage.PayerId,
                payerName = insuranceCoverage.PayerName,
                memberId = insuranceCoverage.MemberId,
                groupNumber = insuranceCoverage.GroupNumber,
                subscriberName = insuranceCoverage.SubscriberName,
                relationshipCode = insuranceCoverage.RelationshipCode,
                subscriberFirstName = insuranceCoverage.SubscriberFirstName,
                subscriberLastName = insuranceCoverage.SubscriberLastName,
                subscriberMiddleName = insuranceCoverage.SubscriberMiddleName,
                subscriberDateOfBirth = insuranceCoverage.SubscriberDateOfBirthFormatted,
                subscriberGender = insuranceCoverage.SubscriberGender,
            }), "insurance-coverage", ct);
            var insuranceCoverageProjectorDocId = await RequireProjectionAsync("InsuranceCoverage837P", ct);
            invoice = await dsl.RunSingleAsync($"{insuranceCoverageDocId} (P) {insuranceCoverageProjectorDocId} (AM) {invoice}", caseId, ct);
        }
        else
        {
            logger.LogWarning("  No InsuranceCoverage row for case {CaseId} — SBR will be empty.", caseId);
        }

        // Payer EDI — the payer's own EDI identifier (2010BB.NM1.NM108/NM109).
        if (insuranceCoverage is not null && payerId is not null)
        {
            logger.LogInformation("Payer EDI: loading for PayerId {PayerId}...", payerId);
            var payerEdi = await data.GetPayerEdiAsync(payerId.Value, ct);
            if (payerEdi is not null)
            {
                var payerDocId = await dsl.SaveSnapshotAsync(JsonSerializer.Serialize(new
                {
                    payerIdentifier = payerEdi.PayerIdentifier,
                    payerIdentifierQualifier = payerEdi.PayerIdentifierQualifier,
                }), "payer-edi", ct);
                var payerProjectorDocId = await RequireProjectionAsync("Payer837P", ct);
                invoice = await dsl.RunSingleAsync($"{payerDocId} (P) {payerProjectorDocId} (AM) {invoice}", caseId, ct);
            }
            else
            {
                logger.LogWarning("  No active PayerEDI row for PayerId {PayerId}.", payerId);
            }
        }

        // Patient (loop 2010CA) — populated only when the patient isn't the subscriber
        // (RelationshipCode 18 = Self). This is the only place RelationshipCode is
        // interpreted; X12Writer just reacts to whether 2010CA has content.
        int? patientId = null;
        if (insuranceCoverage?.RelationshipCode is { } relCode && relCode != "18")
        {
            logger.LogInformation("Patient (2010CA): relationshipCode={RelationshipCode}, loading for case {CaseId}...", relCode, caseId);
            var patient = await data.GetPatientAsync(caseId, ct);
            if (patient is not null)
            {
                patientId = patient.PatientId;
                var patientDocId = await dsl.SaveSnapshotAsync(JsonSerializer.Serialize(new
                {
                    patientId = patient.PatientId,
                    firstName = patient.FirstName,
                    lastName = patient.LastName,
                    dateOfBirth = patient.DateOfBirthFormatted,
                    gender = patient.Gender,
                }), "patient", ct);
                var patientProjectorDocId = await RequireProjectionAsync("Patient837P", ct);
                invoice = await dsl.RunSingleAsync($"{patientDocId} (P) {patientProjectorDocId} (AM) {invoice}", caseId, ct);
            }
            else
            {
                logger.LogWarning("  No Patient row for case {CaseId}.", caseId);
            }
        }
        else
        {
            logger.LogInformation("Patient (2010CA): relationshipCode=18 (self) or no InsuranceCoverage — skipping.");
        }

        // Practice Configuration — runs last among (P)(AM) sources so its submitter/receiver
        // identity (1000A/1000B) wins over any overlapping values.
        logger.LogInformation("Practice Configuration: loading active row...");
        var practiceConfig = await data.GetPracticeConfigurationAsync(ct);

        // Payer-specific Availity trading-partner identity overrides the practice-wide
        // defaults above — only when cases.Payer actually has real values for this payer.
        if (payerId is not null)
        {
            var payerEdiIds = await data.GetPayerEdiIdentifiersAsync(payerId.Value, ct);
            if (!string.IsNullOrWhiteSpace(payerEdiIds?.ReceiverId))
            {
                practiceConfig.ReceiverId = payerEdiIds.ReceiverId;
                practiceConfig.ReceiverIdQualifier = "ZZ";
                logger.LogInformation("  receiverId <- Payer {PayerId} EDIReceiverId ({ReceiverId})", payerId, payerEdiIds.ReceiverId);
            }
            if (!string.IsNullOrWhiteSpace(payerEdiIds?.SubmitterId))
            {
                practiceConfig.SenderId = payerEdiIds.SubmitterId;
                logger.LogInformation("  senderId <- Payer {PayerId} AvailitySubmitterId ({SubmitterId})", payerId, payerEdiIds.SubmitterId);
            }
        }

        var practiceConfigDocId = await dsl.SaveSnapshotAsync(JsonSerializer.Serialize(new
        {
            submitterName = practiceConfig.SubmitterName,
            submitterIdentifier = practiceConfig.SubmitterIdentifier,
            senderIdQualifier = practiceConfig.SenderIdQualifier,
            senderId = practiceConfig.SenderId,
            receiverIdQualifier = practiceConfig.ReceiverIdQualifier,
            receiverId = practiceConfig.ReceiverId,
            functionalIdentifierCode = practiceConfig.FunctionalIdentifierCode,
            versionIdentifier = practiceConfig.VersionIdentifier,
            testIndicator = practiceConfig.TestIndicator,
            billingProviderName = practiceConfig.BillingProviderName,
            billingProviderNPI = practiceConfig.BillingProviderNPI,
            billingProviderTaxonomy = practiceConfig.BillingProviderTaxonomy,
            taxId = practiceConfig.TaxId,
            address1 = practiceConfig.Address1,
            address2 = practiceConfig.Address2,
            city = practiceConfig.City,
            state = practiceConfig.State,
            zip = practiceConfig.Zip,
        }), "practice-configuration", ct);
        var practiceConfigProjectorDocId = await RequireProjectionAsync("PracticeConfiguration837P", ct);
        invoice = await dsl.RunSingleAsync($"{practiceConfigDocId} (P) {practiceConfigProjectorDocId} (AM) {invoice}", caseId, ct);

        // Sources — paper trail of every reference doc used to build this claim.
        logger.LogInformation("Sources: recording paper trail...");
        var sourceDocs = new List<int?>();
        foreach (var sessionDocId in spec.Sessions)
            sourceDocs.Add(await data.GetSessionSourceDocIdAsync(sessionDocId, ct));

        var sourcesDocId = await dsl.SaveSnapshotAsync(JsonSerializer.Serialize(new
        {
            sessions = spec.Sessions,
            sourceDocs,
            renderingProviderId,
            payerId,
            authorization = spec.Authorization,
            patientId,
            insuranceCoverageId = insuranceCoverage?.InsuranceCoverageId,
            practiceConfiguration = practiceConfigDocId,
        }), "claim-sources", ct);
        logger.LogInformation("  sources doc: {SourcesDocId}", sourcesDocId);

        // Fee Schedule + rendering-provider override — both resolved per session: procedure
        // codes and clinicians can differ session to session. (M) merged onto a session-
        // specific snapshot so (Q)'s forEach picks up feeSchedule.minutesPerUnit/allowedAmount
        // and — only when this line's provider differs from the claim-level 2310B primary —
        // renderingProvider.*, which drives a 2420A override for just that line.
        logger.LogInformation("Fee Schedule + Rendering Provider overrides: resolving per session...");
        var sessionDocIdsForBilling = new List<int>();
        foreach (var sessionDocId in spec.Sessions)
        {
            var sessionDoc = await dsl.GetDocumentAsync(sessionDocId, ct);
            var sessionNode = JsonNode.Parse(sessionDoc.Content);
            var serviceCode = sessionNode?["service"]?["code"]?.ToString();

            var feeScheduleLine = payerId is not null && !string.IsNullOrWhiteSpace(serviceCode)
                ? await data.GetFeeScheduleLineAsync(payerId.Value, serviceCode, ct)
                : null;
            if (feeScheduleLine is null)
                logger.LogWarning("  Session {SessionDocId}: no active FeeScheduleLine for PayerId {PayerId} / code '{ServiceCode}' — billing math will fail validation.", sessionDocId, payerId, serviceCode);

            var lineRenderingProvider = await ResolveRenderingProviderAsync(sessionDocId, ct);
            var needsOverride = lineRenderingProvider is not null && lineRenderingProvider.ProviderId != renderingProviderId;

            var mergeFragment = new JsonObject();
            if (feeScheduleLine is not null)
                mergeFragment["feeSchedule"] = new JsonObject
                {
                    ["minutesPerUnit"] = feeScheduleLine.MinutesPerUnit,
                    ["allowedAmount"] = feeScheduleLine.AllowedAmount,
                };
            if (needsOverride)
            {
                mergeFragment["renderingProvider"] = new JsonObject
                {
                    ["providerId"] = lineRenderingProvider!.ProviderId,
                    ["firstName"] = lineRenderingProvider.FirstName,
                    ["lastName"] = lineRenderingProvider.LastName,
                    ["npi"] = lineRenderingProvider.NPI,
                    ["taxonomyCode"] = lineRenderingProvider.TaxonomyCode,
                };
                logger.LogInformation("  Session {SessionDocId}: provider differs from claim primary -> 2420A override ({LastName}, {FirstName})",
                    sessionDocId, lineRenderingProvider.LastName, lineRenderingProvider.FirstName);
            }

            if (mergeFragment.Count > 0)
            {
                var mergeDocId = await dsl.SaveSnapshotAsync(mergeFragment.ToJsonString(), "session-line-context", ct);
                var mergedSessionDocId = await dsl.RunSingleAsync($"{mergeDocId} (M) {sessionDocId}", caseId, ct);
                logger.LogInformation("  Session {SessionDocId} ({ServiceCode}): context ({MergeDocId}) (M) {SessionDocId} => {MergedSessionDocId}",
                    sessionDocId, serviceCode, mergeDocId, mergedSessionDocId);
                sessionDocIdsForBilling.Add(mergedSessionDocId);
            }
            else
            {
                sessionDocIdsForBilling.Add(sessionDocId);
            }
        }

        // Metadata — build from spec and (M) merge into final invoice.
        logger.LogInformation("Metadata: building from spec...");
        var pipelineRunId = Guid.NewGuid().ToString();
        var metadataPatch = new JsonObject
        {
            ["metadata"] = new JsonObject
            {
                ["caseId"] = caseId,
                ["claimNumber"] = claim.ClaimNumber,
                ["pipelineRunId"] = pipelineRunId,
                ["sources"] = new JsonObject
                {
                    ["sessions"] = new JsonArray(sessionDocIdsForBilling.Select(id => (JsonNode)new JsonObject { ["documentId"] = id }).ToArray()),
                    ["authorization"] = spec.Authorization,
                    ["patientId"] = patientId,
                },
            },
        };
        var metaDocId = await dsl.SaveSnapshotAsync(metadataPatch.ToJsonString(), "invoice-metadata", ct);
        logger.LogInformation("  metadata doc: {MetaDocId}", metaDocId);

        invoice = await dsl.RunSingleAsync($"{metaDocId} (M) {invoice}", caseId, ct);
        logger.LogInformation("=== Final invoice (pre-rule) === docId: {Invoice}", invoice);

        // Apply the current 837P rule — split from (W) so we can inspect validation issues
        // before deciding the claim's status.
        var ruleDocId = await data.GetActiveRuleDocIdAsync("837P_LoopsSegments_X12", ct)
            ?? throw new InvalidOperationException("No active RuleDocumentId found for '837P_LoopsSegments_X12'.");
        var ruledClaimDocId = await dsl.RunSingleAsync($"{invoice} (Q) {ruleDocId}", caseId, ct);

        var ruledClaimDoc = await dsl.GetDocumentAsync(ruledClaimDocId, ct);
        var ruledClaimNode = JsonNode.Parse(ruledClaimDoc.Content);
        var issues = ruledClaimNode?["metadata"]?["validationIssues"]?.AsArray()
            .Select(n => n?.ToString() ?? "").ToList() ?? [];
        var claimStatus = issues.Count > 0 ? "HasErrors" : "ReadyToSubmit";

        await data.AddClaimPipelineEventAsync(caseId, claim.ClaimId, queueClaimId, "ClaimValidated", ruledClaimDocId,
            issues.Count > 0 ? string.Join("; ", issues) : "Passed", ct);

        var ediDocId = await dsl.RunSingleAsync($"{ruledClaimDocId} (W)", caseId, ct);

        // Availity Claim Form Preview — same source as the X12, rendered for eyeball verification.
        var availityProjectionDocId = await RequireProjectionAsync("Availity_ProfessionalClaimForm", ct);
        var availityReviewDocId = await dsl.RunSingleAsync($"{ruledClaimDocId} (P) {availityProjectionDocId} (Y)", caseId, ct);

        // Close the loop — link the claim row to its generated EDI + Availity review documents.
        await data.SetClaimEdiDocumentAsync(claim.ClaimId, ediDocId, claimStatus, sourcesDocId, availityReviewDocId, ct);
        await data.AddClaimPipelineEventAsync(caseId, claim.ClaimId, queueClaimId, "EdiGenerated", ediDocId, $"Status={claimStatus}", ct);
        await data.AddClaimPipelineEventAsync(caseId, claim.ClaimId, queueClaimId, "AvailityReviewGenerated", availityReviewDocId, null, ct);

        // Queue2 — hand off to the clearinghouse submission job, but only if validation passed.
        if (claimStatus == "ReadyToSubmit")
        {
            await data.AddClaimToSubmitQueueAsync(claim.ClaimId, ediDocId, ct);
            await data.AddClaimPipelineEventAsync(caseId, claim.ClaimId, queueClaimId, "QueuedForClearingHouse", ediDocId, null, ct);
        }

        // Queue1 — this row has been processed; it's no longer "Pending".
        await data.SetQueueClaimStatusAsync(queueClaimId, "Claim for Clearing House", ct);
        await data.AddClaimPipelineEventAsync(caseId, claim.ClaimId, queueClaimId, "QueueClaimProcessed", null, "Status -> Claim for Clearing House", ct);

        logger.LogInformation("=== Final EDI === claimId={ClaimId} claimNumber={ClaimNumber} status={Status} docId={EdiDocId}",
            claim.ClaimId, claim.ClaimNumber, claimStatus, ediDocId);
        if (issues.Count > 0)
            foreach (var issue in issues) logger.LogWarning("  issue: {Issue}", issue);

        return new ClearingHouseResult(claim.ClaimId, claim.ClaimNumber, ruleDocId, sourcesDocId, ediDocId, availityReviewDocId, claimStatus, issues);
    }

    // If the session was signed by an RBT, returns the supervising BCBA instead — the RBT who
    // actually performed the service is a distinct "performing provider" concept, not the
    // billable rendering provider.
    private async Task<ProviderInfo?> ResolveRenderingProviderAsync(int sessionJsonDocId, CancellationToken ct)
    {
        var sessionDoc = await dsl.GetDocumentAsync(sessionJsonDocId, ct);
        var sessionNode = JsonNode.Parse(sessionDoc.Content);
        var username = sessionNode?["signature"]?["signedByIdentifier"]?.ToString();
        if (string.IsNullOrWhiteSpace(username)) return null;

        var provider = await data.GetProviderByClinicianUsernameAsync(username, ct);
        if (provider is null) return null;

        return provider.ProviderRole == "RBT" ? await data.GetPracticeBcbaAsync(ct) : provider;
    }

    private async Task<int> RequireProjectionAsync(string projectionName, CancellationToken ct) =>
        await data.GetActiveProjectionDocIdAsync(projectionName, ct)
            ?? throw new InvalidOperationException($"No active ProjectionDocumentId found for '{projectionName}'.");
}
