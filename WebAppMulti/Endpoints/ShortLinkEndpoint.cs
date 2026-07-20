using Microsoft.Data.SqlClient;

public static class ShortLinkEndpoint
{
    public static void MapShortLinkEndpoint(this WebApplication app)
    {
        app.MapGet("/s/{id:int}", async (int id, IConfiguration config) =>
        {
            var connStr = config.GetConnectionString("DefaultConnection");
            await using var conn = new SqlConnection(connStr);
            await conn.OpenAsync();

            string? targetUrl = null;
            DateTime? expiresDate = null;

            using (var cmd = new SqlCommand(
                "SELECT TargetUrl, ExpiresDate FROM [cases].[ShortLink] WHERE ShortLinkId = @Id", conn))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    targetUrl = reader.GetString(0);
                    expiresDate = reader.IsDBNull(1) ? null : reader.GetDateTime(1);
                }
            }

            if (targetUrl is null)
                return Results.NotFound("Short link not found.");

            if (expiresDate is not null && expiresDate < DateTime.UtcNow)
                return Results.Text("This link has expired.", statusCode: StatusCodes.Status410Gone);

            using (var updateCmd = new SqlCommand(
                "UPDATE [cases].[ShortLink] SET HitCount = HitCount + 1 WHERE ShortLinkId = @Id", conn))
            {
                updateCmd.Parameters.AddWithValue("@Id", id);
                await updateCmd.ExecuteNonQueryAsync();
            }

            return Results.Redirect(targetUrl);
        });
    }
}
