using Business.Models.Domain;

namespace Business.Services;
public interface IDocumentService
{
	Task AddDocument(Document document);
	Task<Document> GetDocumentById(uint id);
	Task DeleteDocumentById(uint id);
	Task<List<Document>> GetAllDocuments();
}
