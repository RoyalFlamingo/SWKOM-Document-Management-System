using AutoMapper;
using Business.Models.Domain;
using Microsoft.EntityFrameworkCore;
using Persistence;
using Persistence.Models.Entities;

namespace Business.Services;

public class DocumentService(DocumentsDbContext dbContext, IMapper mapper) : IDocumentService
{
	private readonly DocumentsDbContext _dbContext = dbContext;
	private readonly IMapper _mapper = mapper;

	public async Task AddDocument(Document document)
	{
		var documentEntity = _mapper.Map<DocumentEntity>(document);

		_dbContext.Documents.Add(documentEntity);
		await _dbContext.SaveChangesAsync();
	}

	public async Task DeleteDocumentById(uint id)
	{
		var document = await _dbContext.Documents.Where(x => x.Id == id).FirstOrDefaultAsync();

		if (document == null)
			throw new KeyNotFoundException($"Document {id} not found, unable to delete.");

		_dbContext.Documents.Remove(document);
		await _dbContext.SaveChangesAsync();
	}

	public async Task<List<Document>> GetAllDocuments()
	{
		var documentEntities = await _dbContext.Documents.ToListAsync();
		return _mapper.Map<List<Document>>(documentEntities);
	}

	public async Task<Document> GetDocumentById(uint id)
	{
		var document = await _dbContext.Documents.Where(x => x.Id == id).FirstOrDefaultAsync();

		if (document == null)
		{
			throw new KeyNotFoundException($"Document with ID {id} not found.");
		}

		return _mapper.Map<Document>(document);
	}
}

