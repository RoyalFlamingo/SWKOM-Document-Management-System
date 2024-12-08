using Xunit;
using Moq;
using AutoMapper;
using Web.Controllers;
using Business.Models.Domain;
using Business.Services;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Business.Mapping.Profiles;

namespace DocumentManagementSystem.Tests;

public class DocumentsControllerTests
{
	private readonly Mock<IDocumentService> _mockDocumentService;
	private readonly Mock<IRabbitMQService> _mockMQRabbitService;
	private readonly Mock<IElasticService> _mockElasticService;
	private readonly Mock<IMinioService> _mockMinioService;
	private readonly Mock<ILogger<DocumentsController>> _mockLogger;
	private readonly IMapper _mapper;
	private readonly DocumentsController _controller;

	public DocumentsControllerTests()
	{
		_mockDocumentService = new Mock<IDocumentService>();
		_mockLogger = new Mock<ILogger<DocumentsController>>();
		_mockMQRabbitService = new Mock<IRabbitMQService>();
		_mockMinioService = new Mock<IMinioService>();
		_mockElasticService = new Mock<IElasticService>();

		// AutoMapper configuration for the tests
		var config = new MapperConfiguration(cfg =>
		{
			cfg.AddProfile(new DocumentDtoMappingProfile());
			cfg.AddProfile(new DocumentToDocumentEntityMappingProfile());
		});

		_mapper = config.CreateMapper();

		_controller = new DocumentsController(_mockLogger.Object, _mockDocumentService.Object, _mockMQRabbitService.Object, _mockMinioService.Object, _mockElasticService.Object, _mapper);
	}

	[Fact]
	public async Task UploadDocumentAsync_ValidFile_ReturnsOk()
	{
		// Arrange
		var fileMock = new Mock<IFormFile>();
		fileMock.Setup(f => f.FileName).Returns("test.pdf");
		fileMock.Setup(f => f.Length).Returns(100);

		var documentUploadDto = new DocumentUploadDto
		{
			File = fileMock.Object
		};

		// Act
		var result = await _controller.UploadDocumentAsync(documentUploadDto);

		// Assert
		var okResult = Assert.IsType<OkObjectResult>(result);
		Assert.NotNull(okResult);
	}

	[Fact]
	public async Task UploadDocumentAsync_EmptyFile_ReturnsBadRequest()
	{
		// Arrange
		var fileMock = new Mock<IFormFile>();
		fileMock.Setup(f => f.FileName).Returns(string.Empty);
		fileMock.Setup(f => f.Length).Returns(0);

		var documentUploadDto = new DocumentUploadDto
		{
			File = fileMock.Object
		};

		// Add validation error
		_controller.ModelState.AddModelError("File", "The uploaded file must not be empty.");

		// Act
		var result = await _controller.UploadDocumentAsync(documentUploadDto);

		// Assert
		var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
		Assert.NotNull(badRequestResult);
	}

	[Fact]
	public async Task GetDocumentById_ValidId_ReturnsOk()
	{
		// Arrange
		var document = new Document { Id = 1, Name = "Test Doc", Size = 123, UploadedAt = DateTime.UtcNow };
		_mockDocumentService.Setup(service => service.GetDocumentByIdAsync(1))
			.ReturnsAsync(document);

		// Act
		var result = await _controller.GetDocumentById(1);

		// Assert
		var okResult = Assert.IsType<OkObjectResult>(result.Result);
		var returnedDocument = Assert.IsType<DocumentResponseDto>(okResult.Value);
		Assert.Equal("Test Doc", returnedDocument.Name);
		Assert.Equal(1, returnedDocument.Id);
	}

	[Fact]
	public async Task GetDocumentById_InvalidId_ReturnsNotFound()
	{
		// Arrange
		_mockDocumentService.Setup(service => service.GetDocumentByIdAsync(It.IsAny<uint>()))
			.Throws(new KeyNotFoundException());

		// Act
		var result = await _controller.GetDocumentById(100);

		// Assert
		var notFoundResult = Assert.IsType<NotFoundResult>(result.Result);
		Assert.NotNull(notFoundResult);
	}

	[Fact]
	public async Task ListDocumentsAsync_ReturnsDocuments()
	{
		// Arrange
		var documents = new List<Document>
	{
		new Document { Id = 1, Name = "Doc1", Size = 100, UploadedAt = DateTime.UtcNow },
		new Document { Id = 2, Name = "Doc2", Size = 200, UploadedAt = DateTime.UtcNow }
	};
		_mockDocumentService.Setup(service => service.GetAllDocumentsAsync())
			.ReturnsAsync(documents);

		// Act
		var result = await _controller.ListDocumentsAsync();

		// Assert
		var okResult = Assert.IsType<OkObjectResult>(result);
		var returnedDocuments = Assert.IsType<List<DocumentResponseDto>>(okResult.Value);
		Assert.Equal(2, returnedDocuments.Count);
	}

	[Fact]
	public async Task ListDocumentsAsync_NoDocuments_ReturnsEmptyList()
	{
		// Arrange
		_mockDocumentService.Setup(service => service.GetAllDocumentsAsync())
			.ReturnsAsync(new List<Document>());

		// Act
		var result = await _controller.ListDocumentsAsync();

		// Assert
		var okResult = Assert.IsType<OkObjectResult>(result);
		var returnedDocuments = Assert.IsType<List<DocumentResponseDto>>(okResult.Value);
		Assert.Empty(returnedDocuments);
	}

	[Fact]
	public async Task DeleteDocumentAsync_ValidId_ReturnsOk()
	{
		// Arrange
		_mockDocumentService.Setup(service => service.DeleteDocumentByIdAsync(1))
			.Returns(Task.CompletedTask);

		// Act
		var result = await _controller.DeleteDocumentAsync(1);

		// Assert
		var okResult = Assert.IsType<OkObjectResult>(result);
		Assert.NotNull(okResult);
	}

	[Fact]
	public async Task DeleteDocumentAsync_InvalidId_ReturnsNotFound()
	{
		// Arrange
		_mockDocumentService.Setup(service => service.DeleteDocumentByIdAsync(It.IsAny<uint>()))
			.Throws(new KeyNotFoundException());

		// Act
		var result = await _controller.DeleteDocumentAsync(100);

		// Assert
		var notFoundResult = Assert.IsType<NotFoundResult>(result);
		Assert.NotNull(notFoundResult);
	}

}