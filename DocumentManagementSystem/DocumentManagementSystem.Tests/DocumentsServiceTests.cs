using AutoMapper;
using Business.Models.Domain;
using Business.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Persistence;
using Persistence.Models.Entities;
using Xunit;

namespace DocumentManagementSystem.Tests;

public class DocumentServiceTests : IClassFixture<InMemoryDatabaseFixture>
{
	private readonly DocumentsDbContext _dbContext;
	private readonly Mock<ILogger<DocumentService>> _mockLogger;
	private readonly IMapper _mapper;
	private readonly DocumentService _documentService;

	public DocumentServiceTests(InMemoryDatabaseFixture fixture)
	{
		// Verwende den DbContext aus der Fixture
		_dbContext = fixture.DbContext;

		// AutoMapper configuration for testing
		var config = new MapperConfiguration(cfg =>
		{
			cfg.CreateMap<Document, DocumentEntity>().ReverseMap();
		});

		_mapper = config.CreateMapper();
		_mockLogger = new Mock<ILogger<DocumentService>>();

		_documentService = new DocumentService(_dbContext, _mapper, _mockLogger.Object);
	}

	//reset the database before each test to ensure a constant state
	private void ResetDatabase()
	{
		_dbContext.Documents.RemoveRange(_dbContext.Documents);
		_dbContext.SaveChanges();
	}

	[Fact]
	public async Task AddDocumentAsync_ValidDocument_AddsToDatabase()
	{
		// Arrange
		ResetDatabase();
		var document = new Document { Name = "Test Document", Size = 1024, UploadedAt = DateTime.UtcNow };

		// Act
		await _documentService.AddDocumentAsync(document);

		// Assert
		var addedDocument = await _dbContext.Documents.FirstOrDefaultAsync(d => d.Name == "Test Document");
		Assert.NotNull(addedDocument);
		Assert.Equal((ulong)1024, addedDocument.Size);
		Assert.NotEqual(DateTime.MinValue, addedDocument.UploadedAt);
	}

	[Fact]
	public async Task DeleteDocumentByIdAsync_ValidId_DeletesDocument()
	{
		// Arrange
		ResetDatabase();
		var document = new DocumentEntity { Name = "ToDelete", Size = 100, UploadedAt = DateTime.UtcNow };
		_dbContext.Documents.Add(document);
		await _dbContext.SaveChangesAsync();

		// Act
		await _documentService.DeleteDocumentByIdAsync(document.Id);

		// Assert
		var deletedDocument = await _dbContext.Documents.FindAsync(document.Id);
		Assert.Null(deletedDocument);
	}

	[Fact]
	public async Task DeleteDocumentByIdAsync_InvalidId_ThrowsException()
	{
		// Act & Assert
		await Assert.ThrowsAsync<KeyNotFoundException>(() => _documentService.DeleteDocumentByIdAsync(9999));
	}

	[Fact]
	public async Task GetAllDocumentsAsync_ReturnsAllDocuments()
	{
		// Arrange
		ResetDatabase();
		var documents = new List<DocumentEntity>
		{
			new DocumentEntity { Name = "Doc1", Size = 100, UploadedAt = DateTime.UtcNow },
			new DocumentEntity { Name = "Doc2", Size = 200, UploadedAt = DateTime.UtcNow }
		};
		_dbContext.Documents.AddRange(documents);
		await _dbContext.SaveChangesAsync();

		// Act
		var result = await _documentService.GetAllDocumentsAsync();

		// Assert
		Assert.Equal(2, result.Count);
		Assert.Contains(result, d => d.Name == "Doc1");
		Assert.Contains(result, d => d.Name == "Doc2");
	}

	[Fact]
	public async Task GetDocumentByIdAsync_ValidId_ReturnsDocument()
	{
		// Arrange
		ResetDatabase();
		var document = new DocumentEntity { Name = "Test Document", Size = 1234, UploadedAt = DateTime.UtcNow };
		_dbContext.Documents.Add(document);
		await _dbContext.SaveChangesAsync();

		// Act
		var result = await _documentService.GetDocumentByIdAsync(document.Id);

		// Assert
		Assert.NotNull(result);
		Assert.Equal("Test Document", result.Name);
		Assert.Equal(1234, result.Size);
	}

	[Fact]
	public async Task GetDocumentByIdAsync_InvalidId_ThrowsException()
	{
		await Assert.ThrowsAsync<KeyNotFoundException>(() => _documentService.GetDocumentByIdAsync(9999));
	}
}
