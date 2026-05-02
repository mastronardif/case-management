using System.Text;

internal static class DeployScriptBuilder
{
    public static async Task BuildAsync(string basePath)
    {
        var deployFolder = Path.Combine(basePath, "Deploy");
        Directory.CreateDirectory(deployFolder); // ✅ FIX

        var deployPath = Path.Combine(deployFolder, "deploy.sql");

        var sb = new StringBuilder();

        sb.AppendLine("-- AUTO-GENERATED DEPLOY SCRIPT");
        sb.AppendLine("SET NOCOUNT ON;");
        sb.AppendLine();

        AppendFolder(sb, Path.Combine(basePath, "Tables"));
        AppendFolder(sb, Path.Combine(basePath, "Views"));
        AppendFolder(sb, Path.Combine(basePath, "Functions"));
        AppendFolder(sb, Path.Combine(basePath, "StoredProcedures"));

        await File.WriteAllTextAsync(deployPath, sb.ToString());
    }

    private static void AppendFolder(StringBuilder sb, string folder)
    {
        if (!Directory.Exists(folder))
            return;

        foreach (var file in Directory.GetFiles(folder, "*.sql").OrderBy(f => f))
        {
            sb.AppendLine("-- =====================================");
            sb.AppendLine($"-- FILE: {Path.GetFileName(file)}");
            sb.AppendLine("-- =====================================");
            sb.AppendLine(File.ReadAllText(file));
            sb.AppendLine("GO");
            sb.AppendLine();
        }
    }
}
