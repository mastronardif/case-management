using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

public class ProgramsToCsv
{
    public static void MainRun()
    {
        const string FPATH = ".\\Workbooks";
        string jsonPath = Path.Combine(FPATH, "data000.json");
        string csvPath = Path.Combine(FPATH, "output000.csv"); 

        // Read JSON
        string jsonString = File.ReadAllText(jsonPath);
        var records = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(jsonString);

        if (records == null || records.Count == 0)
        {
            Console.WriteLine("No data found in JSON.");
            return;
        }

        // Build CSV
        var sb = new StringBuilder();

        // Write header row (column names from keys of first record)
        var headers = new List<string>(records[0].Keys);
        sb.AppendLine(string.Join(",", headers));

        // Write data rows
        foreach (var record in records)
        {
            var row = new List<string>();
            foreach (var header in headers)
            {
                string value = record.ContainsKey(header) && record[header] != null
                    ? record[header].ToString().Replace("\"", "\"\"")  // escape quotes
                    : "";

                if (value.Contains(",") || value.Contains("\""))
                {
                    value = $"\"{value}\""; // wrap in quotes if needed
                }

                row.Add(value);
            }
            sb.AppendLine(string.Join(",", row));
        }

        // Save to CSV
        File.WriteAllText(csvPath, sb.ToString());
        Console.WriteLine($"CSV created: {csvPath}");
    }
}
