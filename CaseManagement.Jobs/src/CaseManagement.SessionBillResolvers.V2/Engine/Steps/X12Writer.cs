using System.Globalization;
using System.Text;
using System.Text.Json;

namespace CaseManagement.SessionBillResolvers.V2.Engine.Steps;

// Mechanical serializer: 837P_LoopsSegments_X12 JSON (loops.*) -> fixed-layout X12 005010X222A1 text.
// No business rules here — field values/defaults belong in the projector/rule steps upstream.
public class X12Writer : IX12Writer
{
    private const char ElementSeparator = '*';
    private const char CompositeSeparator = ':';
    private const char SegmentSeparator = '~';

    private const string SenderId = "SENDERID";
    private const string ReceiverId = "RECEIVERID";
    private const string InterchangeControlNumber = "000000001";
    private const string GroupControlNumber = "1";
    private const string TransactionControlNumber = "0001";
    private const string ProviderTaxonomyCodeListQualifier = "PXC";
    private const string ClmFacilityCodeQualifier = "B";
    private const string ClmFrequencyCodeOriginal = "1"; // Medicare CG: CLM05-3 must equal "1" (ORIGINAL)

    public string Build(string payload)
    {
        var root = JsonDocument.Parse(payload).RootElement;
        var loops = GetObject(root, "loops");
        var metadata = GetObject(root, "metadata");
        var now = DateTime.Now;

        var sb = new StringBuilder();
        var segmentCount = 0;

        void Write(string segmentId, params string[] elements)
        {
            sb.Append(BuildSegment(segmentId, elements));
            sb.AppendLine();
            segmentCount++;
        }

        // Envelope (ISA is fixed-width per spec; not trimmed)
        sb.AppendLine(BuildFixedSegment("ISA",
            "00", "          ", "00", "          ",
            "ZZ", SenderId.PadRight(15), "ZZ", ReceiverId.PadRight(15),
            now.ToString("yyMMdd"), now.ToString("HHmm"), "^", "00501",
            InterchangeControlNumber, "0", "T", ":"));

        Write("GS", "HC", SenderId, ReceiverId, now.ToString("yyyyMMdd"), now.ToString("HHmm"),
            GroupControlNumber, "X", "005010X222A1");

        var transactionStart = segmentCount; // segments from here through SE (inclusive) count toward SE01
        Write("ST", "837", TransactionControlNumber, "005010X222A1");
        Write("BHT", "0019", "00", GetString(metadata, "claimNumber"), now.ToString("yyyyMMdd"), now.ToString("HHmm"), "CH");

        WriteNM1(Write, GetObject(loops, "1000A"), "NM1");
        WriteNM1(Write, GetObject(loops, "1000B"), "NM1");

        WriteHL(Write, GetObject(loops, "2000A"));

        var loop2010AA = GetObject(loops, "2010AA");
        WritePrv(Write, loop2010AA, "BI"); // Billing Provider Specialty — belongs to 2000A, precedes NM1
        WriteNM1(Write, loop2010AA, "NM1");
        WriteN3(Write, loop2010AA);
        WriteN4(Write, loop2010AA);
        WriteRef(Write, GetObject(loop2010AA, "REF_EI"), "EI");

        WriteHL(Write, GetObject(loops, "2000B"));
        WriteSbr(Write, GetObject(loops, "2000B"));

        var loop2010BA = GetObject(loops, "2010BA");
        WriteNM1(Write, loop2010BA, "NM1");
        WriteDmg(Write, loop2010BA);

        WriteNM1(Write, GetObject(loops, "2010BB"), "NM1");

        var loop2300 = GetObject(loops, "2300");
        WriteClm(Write, GetObject(loop2300, "CLM"));
        WriteHi(Write, GetObject(loop2300, "HI"));
        WriteRef(Write, GetObject(loop2300, "REF_G1"), "G1");

        var loop2310B = GetObject(loops, "2310B");
        WriteNM1(Write, loop2310B, "NM1");
        WritePrv(Write, loop2310B, "PE"); // Rendering Provider Specialty — follows NM1 in 2310B

        foreach (var line in EnumerateArray(GetProperty(loops, "2400")))
        {
            WriteLx(Write, GetObject(line, "LX"));
            WriteSv1(Write, GetObject(line, "SV1"));
            WriteDtp(Write, GetObject(line, "DTP472"), "472");
        }

        var transactionSegments = (segmentCount - transactionStart) + 1; // +1 for SE itself
        Write("SE", transactionSegments.ToString(CultureInfo.InvariantCulture), TransactionControlNumber);
        Write("GE", "1", GroupControlNumber);
        sb.AppendLine(BuildFixedSegment("IEA", "1", InterchangeControlNumber));

        return sb.ToString();
    }

    // ── Segment builders (one per loop's segment shape) ─────────────────────

    private static void WriteNM1(Action<string, string[]> write, JsonElement nm1Loop, string childName)
    {
        var nm1 = GetObject(nm1Loop, childName);
        if (nm1.ValueKind != JsonValueKind.Object) return;

        write("NM1", [
            GetString(nm1, "NM101"), GetString(nm1, "NM102"), GetString(nm1, "NM103"), GetString(nm1, "NM104"),
            "", "", "", GetString(nm1, "NM108"), GetString(nm1, "NM109")
        ]);
    }

    private static void WriteN3(Action<string, string[]> write, JsonElement loop)
    {
        var n3 = GetObject(loop, "N3");
        if (n3.ValueKind != JsonValueKind.Object) return;
        write("N3", [GetString(n3, "N301"), GetString(n3, "N302")]);
    }

    private static void WriteN4(Action<string, string[]> write, JsonElement loop)
    {
        var n4 = GetObject(loop, "N4");
        if (n4.ValueKind != JsonValueKind.Object) return;
        write("N4", [GetString(n4, "N401"), GetString(n4, "N402"), GetString(n4, "N403")]);
    }

    private static void WriteRef(Action<string, string[]> write, JsonElement refLoop, string qualifier)
    {
        if (refLoop.ValueKind != JsonValueKind.Object) return;
        var value = GetString(refLoop, "REF02");
        if (value.Length == 0) return;
        write("REF", [qualifier, value]);
    }

    private static void WriteHL(Action<string, string[]> write, JsonElement loop)
    {
        var hl = GetObject(loop, "HL");
        if (hl.ValueKind != JsonValueKind.Object) return;
        write("HL", [GetString(hl, "HL01"), GetString(hl, "HL02"), GetString(hl, "HL03"), GetString(hl, "HL04")]);
    }

    private static void WriteSbr(Action<string, string[]> write, JsonElement loop)
    {
        var sbr = GetObject(loop, "SBR");
        if (sbr.ValueKind != JsonValueKind.Object) return;
        write("SBR", [
            GetString(sbr, "SBR01"), GetString(sbr, "SBR02"), GetString(sbr, "SBR03"), GetString(sbr, "SBR04"),
            GetString(sbr, "SBR05"), GetString(sbr, "SBR06"), GetString(sbr, "SBR07"), GetString(sbr, "SBR08"),
            GetString(sbr, "SBR09")
        ]);
    }

    private static void WriteDmg(Action<string, string[]> write, JsonElement loop)
    {
        var dmg = GetObject(loop, "DMG");
        if (dmg.ValueKind != JsonValueKind.Object) return;
        write("DMG", ["D8", GetString(dmg, "DMG02"), GetString(dmg, "DMG03")]);
    }

    private static void WritePrv(Action<string, string[]> write, JsonElement loop, string entityTypeQualifier)
    {
        var prv = GetObject(loop, "PRV");
        if (prv.ValueKind != JsonValueKind.Object) return;
        var taxonomy = GetString(prv, "PRV03");
        if (taxonomy.Length == 0) return;
        write("PRV", [entityTypeQualifier, ProviderTaxonomyCodeListQualifier, taxonomy]);
    }

    private static void WriteClm(Action<string, string[]> write, JsonElement clm)
    {
        if (clm.ValueKind != JsonValueKind.Object) return;
        var placeOfService = GetString(clm, "CLM05_1");
        var clm05 = placeOfService.Length > 0
            ? $"{placeOfService}{CompositeSeparator}{ClmFacilityCodeQualifier}{CompositeSeparator}{ClmFrequencyCodeOriginal}"
            : "";

        write("CLM", [
            GetString(clm, "CLM01"), GetMoney(clm, "CLM02"), "", "", clm05
        ]);
    }

    private static void WriteHi(Action<string, string[]> write, JsonElement hi)
    {
        if (hi.ValueKind != JsonValueKind.Object) return;
        var code = GetString(hi, "HI01_1");
        var value = GetString(hi, "HI01_2");
        if (value.Length == 0) return;
        write("HI", [$"{code}{CompositeSeparator}{value}"]);
    }

    private static void WriteLx(Action<string, string[]> write, JsonElement lx)
    {
        if (lx.ValueKind != JsonValueKind.Object) return;
        write("LX", [GetInt(lx, "LX01")]);
    }

    private static void WriteSv1(Action<string, string[]> write, JsonElement sv1)
    {
        if (sv1.ValueKind != JsonValueKind.Object) return;
        var composite = string.Join(CompositeSeparator,
            GetString(sv1, "SV101_1"), GetString(sv1, "SV101_2"), GetString(sv1, "SV101_3"));

        write("SV1", [composite, GetMoney(sv1, "SV102"), GetString(sv1, "SV103"), GetInt(sv1, "SV104")]);
    }

    private static void WriteDtp(Action<string, string[]> write, JsonElement dtp, string qualifier)
    {
        if (dtp.ValueKind != JsonValueKind.Object) return;
        var value = GetString(dtp, "DTP03");
        if (value.Length == 0) return;
        write("DTP", [qualifier, "D8", value]);
    }

    // ── JSON access helpers ──────────────────────────────────────────────────

    private static JsonElement GetProperty(JsonElement obj, string name) =>
        obj.ValueKind == JsonValueKind.Object && obj.TryGetProperty(name, out var value) ? value : default;

    private static JsonElement GetObject(JsonElement obj, string name)
    {
        var value = GetProperty(obj, name);
        return value.ValueKind == JsonValueKind.Object ? value : default;
    }

    private static string GetString(JsonElement obj, string name)
    {
        var value = GetProperty(obj, name);
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString() ?? "",
            JsonValueKind.Number => value.GetRawText(),
            _ => ""
        };
    }

    private static string GetMoney(JsonElement obj, string name)
    {
        var value = GetProperty(obj, name);
        return value.ValueKind == JsonValueKind.Number
            ? value.GetDecimal().ToString("0.00", CultureInfo.InvariantCulture)
            : "";
    }

    private static string GetInt(JsonElement obj, string name)
    {
        var value = GetProperty(obj, name);
        return value.ValueKind == JsonValueKind.Number
            ? value.GetDecimal().ToString("0", CultureInfo.InvariantCulture)
            : "";
    }

    private static IEnumerable<JsonElement> EnumerateArray(JsonElement element) =>
        element.ValueKind == JsonValueKind.Array ? element.EnumerateArray() : [];

    // ── Segment assembly ─────────────────────────────────────────────────────

    // Trailing empty elements are omitted (valid per X12 syntax); interior blanks are kept as positional placeholders.
    private static string BuildSegment(string segmentId, IReadOnlyList<string> elements)
    {
        var lastNonEmpty = -1;
        for (var i = 0; i < elements.Count; i++)
            if (elements[i].Length > 0) lastNonEmpty = i;

        var sb = new StringBuilder(segmentId);
        for (var i = 0; i <= lastNonEmpty; i++)
            sb.Append(ElementSeparator).Append(elements[i]);

        sb.Append(SegmentSeparator);
        return sb.ToString();
    }

    // Fixed-width segments (ISA, IEA) always emit every element, no trimming.
    private static string BuildFixedSegment(string segmentId, params string[] elements)
    {
        var sb = new StringBuilder(segmentId);
        foreach (var e in elements)
            sb.Append(ElementSeparator).Append(e);
        sb.Append(SegmentSeparator);
        return sb.ToString();
    }
}
