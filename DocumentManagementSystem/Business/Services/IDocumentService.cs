using Business.Models.Domain;

namespace Business.Services;
public interface IDocumentService
{
	Task AddDocumentAsync(Document document);
	Task<Document> GetDocumentByIdAsync(uint id);
	Task DeleteDocumentByIdAsync(uint id);
	Task<List<Document>> GetAllDocumentsAsync();
}
