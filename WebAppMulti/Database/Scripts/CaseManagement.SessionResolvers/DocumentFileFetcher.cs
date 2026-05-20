using Microsoft.Data.SqlClient;
using System.Data;

namespace CaseManagement.Common;

public static class DocumentFileFetcher
{
    public static async Task<(byte[] FileData, string? FileName)> GetDocumentAsync(
        SqlConnection conn,
        int documentId)    
    {
        // possible use  // usp_Document_GetByContext
        using var cmd = new SqlCommand("[cases].[usp_Document_GetById]", conn);
        cmd.CommandType = CommandType.StoredProcedure;

        cmd.Parameters.Add("@DocumentId", SqlDbType.Int).Value = documentId;

        using var reader = await cmd.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
            throw new Exception($"Document {documentId} not found");

        var fileData = (byte[])reader["FileData"];
        var fileName = reader["FileName"] as string;

        return (fileData, fileName);
    }
}