using WebAppMulti.Database.Dtos.CaseManagement;

namespace WebAppMulti.Services.CaseManagement
{
    public interface ISessionDocumentService
    {
        Task SaveDocument(int sessionId, DocumentCreateDto dto);
        Task<IEnumerable<SessionDocumentDto>> GetDocuments(int sessionId);
    }
}
