using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JsonTo837
{
    // Models -- match the JSON structure you used earlier
    public class ClaimFile
    {
        public Isa isa { get; set; }
        public Submitter submitter { get; set; }
        public Provider provider { get; set; }
        public Patient patient { get; set; }
        public Payer payer { get; set; }
        public Claim claim { get; set; }
    }

    public class Isa
    {
        public string sender_id { get; set; }
        public string receiver_id { get; set; } = "AVAILITY";
    }

    public class Submitter
    {
        public string submitter_id { get; set; }
        public string practice_name { get; set; }
    }

    public class Provider
    {
        public string npi { get; set; }
        public string tax_id { get; set; }
        public string phone { get; set; }
        public string address { get; set; }
        public string city_state_zip { get; set; }
    }

    public class Patient
    {
        public string first { get; set; }
        public string last { get; set; }
        public string member_id { get; set; }
        public string dob { get; set; } // YYYYMMDD
        public string gender { get; set; }
        public string address { get; set; }
        public string city { get; set; }
        public string state { get; set; }
        public string zip { get; set; }
    }

    public class Payer
    {
        public string name { get; set; }
        public string payer_id { get; set; }
    }

    public class Claim
    {
        public string claim_number { get; set; }
        public string total_charge { get; set; }
        public string diagnosis { get; set; }
        public List<ClaimLine> lines { get; set; }
    }

    public class ClaimLine
    {
        public int line { get; set; }
        public string cpt { get; set; }
        public string amount { get; set; }
        public string units { get; set; }
        public string date { get; set; } // YYYYMMDD
    }

    class Program
    {
        const string FPATH = ".\\Workbooks";
        static void Main(string[] args)
        {

            ProgramsToCsv.MainRun(); return;

            // Default input file name (change if you want)
            //string inputJson = "claim837.json";
            string inputJson = Path.Combine(FPATH, "claim837.json");
            string outputEdi = Path.Combine(FPATH, "claim837.txt");

            if (!File.Exists(inputJson))
            {
                Console.WriteLine($"Input JSON not found: {inputJson}");
                Console.WriteLine("Place a JSON file named claim837.json in the same folder as the executable.");
                return;
            }

            string json = File.ReadAllText(inputJson);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            ClaimFile model;

            try
            {
                model = JsonSerializer.Deserialize<ClaimFile>(json, options);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to parse JSON: " + ex.Message);
                return;
            }

            var edi = BuildEdiFromModel(model);
            File.WriteAllText(outputEdi, edi, Encoding.UTF8);
            Console.WriteLine($"Generated EDI written to {outputEdi}");
        }

        static string PadRightFixed(string value, int width)
        {
            if (value == null) value = "";
            if (value.Length >= width) return value.Substring(0, width);
            return value.PadRight(width, ' ');
        }

        static string BuildEdiFromModel(ClaimFile m)
        {
            var now = DateTime.UtcNow; // Use UTC for ISA timestamps commonly
            var localNow = DateTime.Now;
            string isaDateYYMMDD = now.ToString("yyMMdd");
            string isaTimeHHMM = now.ToString("HHmm");
            string gsDate = now.ToString("yyyyMMdd");
            string gsTime = now.ToString("HHmm");
            string controlNumberIsa = "000000905"; // static for example; you might want to generate/increment
            string gsControlNumber = "1";
            string stControlNumber = "0001";

            var segs = new List<string>();

            // ST segment first (it must be counted in SE)
            segs.Add($"ST*837*{stControlNumber}*005010X222A1");

            // BHT
            segs.Add($"BHT*0019*00*{m.claim.claim_number}*{gsDate}*{gsTime}*CH");

            // Submitter (NM1*41)
            segs.Add($"NM1*41*2*{Escape(m.submitter.practice_name)}*****46*{m.submitter.submitter_id}");
            segs.Add($"PER*IC*Billing Contact*TE*{m.provider.phone}");
            segs.Add($"NM1*40*2*{Escape(m.isa.receiver_id)}*****46*AVA001");

            // Billing provider
            segs.Add($"HL*1**20*1");
            segs.Add($"NM1*85*2*{Escape(m.submitter.practice_name)}*****XX*{m.provider.npi}");
            if (!string.IsNullOrWhiteSpace(m.provider.address)) segs.Add($"N3*{Escape(m.provider.address)}");
            if (!string.IsNullOrWhiteSpace(m.provider.city_state_zip)) segs.Add($"N4*{Escape(m.provider.city_state_zip)}");
            if (!string.IsNullOrWhiteSpace(m.provider.tax_id)) segs.Add($"REF*EI*{m.provider.tax_id}");
            segs.Add($"PER*IC*Provider Contact*TE*{m.provider.phone}");

            // Subscriber / patient
            segs.Add($"HL*2*1*22*0");
            segs.Add($"SBR*P*18*******MC");
            segs.Add($"NM1*IL*1*{Escape(m.patient.last)}*{Escape(m.patient.first)}****MI*{m.patient.member_id}");
            if (!string.IsNullOrWhiteSpace(m.patient.address)) segs.Add($"N3*{Escape(m.patient.address)}");
            if (!string.IsNullOrWhiteSpace(m.patient.city) || !string.IsNullOrWhiteSpace(m.patient.state) || !string.IsNullOrWhiteSpace(m.patient.zip))
            {
                segs.Add($"N4*{Escape(m.patient.city)}*{Escape(m.patient.state)}*{Escape(m.patient.zip)}");
            }
            if (!string.IsNullOrWhiteSpace(m.patient.dob) || !string.IsNullOrWhiteSpace(m.patient.gender))
                segs.Add($"DMG*D8*{m.patient.dob}*{m.patient.gender}");

            // Payer info
            segs.Add($"NM1*PR*2*{Escape(m.payer.name)}*****PI*{m.payer.payer_id}");

            // Claim control
            segs.Add($"CLM*{m.claim.claim_number}*{m.claim.total_charge}***11:B:1*Y*A*Y*I");

            // Diagnosis
            if (!string.IsNullOrWhiteSpace(m.claim.diagnosis))
            {
                // using HI*ABK:xxxx (simple)
                segs.Add($"HI*ABK:{m.claim.diagnosis.Replace(".", "")}");
            }

            // Service lines
            if (m.claim.lines != null)
            {
                foreach (var line in m.claim.lines)
                {
                    segs.Add($"LX*{line.line}");
                    segs.Add($"SV1*HC:{line.cpt}*{line.amount}*UN*{line.units}***1");
                    segs.Add($"DTP*472*D8*{line.date}");
                }
            }

            // Now compute SE segment count: number of segments from ST through SE inclusive.
            // We have built segs list which includes ST and all segments except SE.
            int segCountIncludingSE = segs.Count + 1; // +1 for the SE segment itself

            // Build the final EDI string: ISA, GS, then all segments joined with ~, then SE, GE, IEA
            var sb = new StringBuilder();

            // ISA - fixed length fields, pad sender/receiver to 15 chars where appropriate.
            string snd = PadRightFixed(m.isa.sender_id ?? "SENDERID", 15);
            string rcv = PadRightFixed(m.isa.receiver_id ?? "AVAILITY", 15);
            sb.Append($"ISA*00*          *00*          *ZZ*{snd}*ZZ*{rcv}*{isaDateYYMMDD}*{isaTimeHHMM}*^*00501*{controlNumberIsa}*0*T*:\r\n");

            // GS
            sb.Append($"GS*HC*{m.isa.sender_id}*{m.isa.receiver_id}*{gsDate}*{gsTime}*{gsControlNumber}*X*005010X222A1\r\n");

            // Add all body segments (separated by ~ inside, but we'll append one per line for readability, replace with ~ as delimiter)
            for (int i = 0; i < segs.Count; i++)
            {
                // Append segment text + segment terminator ~
                sb.Append(segs[i] + "~\r\n");
            }

            // SE
            sb.Append($"SE*{segCountIncludingSE}*{stControlNumber}~\r\n");

            // GE and IEA
            sb.Append($"GE*1*{gsControlNumber}\r\n");
            sb.Append($"IEA*1*{controlNumberIsa}\r\n");

            // Return final EDI (trim final whitespace)
            return sb.ToString().Trim();
        }

        static string Escape(string s)
        {
            if (s == null) return "";
            // Replace any characters that might break X12: ~ * : ^
            return s.Replace("~", "").Replace("*", "").Replace("^", "").Replace(":", "");
        }
    }
}
