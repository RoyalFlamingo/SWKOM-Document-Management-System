using AutoMapper;
using Business.Models.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Persistence;
using Persistence.Models.Entities;

namespace Business.Services;

public class DocumentService(DocumentsDbContext dbContext, IMapper mapper, ILogger<DocumentService> logger) : IDocumentService
{
	private readonly DocumentsDbContext _dbContext = dbContext;
	private readonly IMapper _mapper = mapper;
	private readonly ILogger<DocumentService> _logger = logger;

	public async Task<uint> AddDocumentAsync(Document document)
	{
		try
		{
			document.UploadedAt = DateTime.UtcNow; // add current date to document

			var documentEntity = _mapper.Map<DocumentEntity>(document);

			_dbContext.Documents.Add(documentEntity);
			await _dbContext.SaveChangesAsync();

			return (uint)documentEntity.Id;
		}
		catch (AutoMapperMappingException ex)
		{
			_logger.LogError(ex, "Error during mapping Document to DocumentEntity");
			throw;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "An error occurred while adding the document to the database");
			throw;
		}
	}

	public async Task DeleteDocumentByIdAsync(uint id)
	{
		var document = await _dbContext.Documents.Where(x => x.Id == id).FirstOrDefaultAsync();

		if (document == null)
			throw new KeyNotFoundException($"Document {id} not found, unable to delete.");

		_dbContext.Documents.Remove(document);
		await _dbContext.SaveChangesAsync();
	}

	public async Task<List<Document>> GetAllDocumentsAsync()
	{
		var documentEntities = await _dbContext.Documents.ToListAsync();
		return _mapper.Map<List<Document>>(documentEntities);
	}

	public async Task<Document> GetDocumentByIdAsync(uint id)
	{
		var document = await _dbContext.Documents.Where(x => x.Id == id).FirstOrDefaultAsync();

		if (document == null)
		{
			throw new KeyNotFoundException($"Document with ID {id} not found.");
		}

		return _mapper.Map<Document>(document);
	}
}

