using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using Persistence.Models.Entities;
using Business.Services;
using Business.Models.Domain;
using AutoMapper;

namespace Web.Controllers
{
	[ApiController]
	[ApiVersion("1")]
	[Route("api/v{version:apiVersion}/[controller]")]
	public class DocumentsController(ILogger<DocumentsController> logger, IDocumentService documentService, 
		IRabbitMQService rabbitMQService, IMinioService minioService, IElasticService elasticService,  IMapper mapper) : ControllerBase
	{
		private readonly ILogger<DocumentsController> _logger = logger;
		private readonly IDocumentService _documentService = documentService;
		private readonly IRabbitMQService _rabbitMQService = rabbitMQService;
		private readonly IMinioService _minioService = minioService;
		private readonly IElasticService _elasticService = elasticService;
		private readonly IMapper _mapper = mapper;

		// Add document to elastic
		[HttpPost("elastic-index", Name = "ElasticIndexDocument")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status500InternalServerError)]
		public async Task<IActionResult> ElasticIndexDocument([FromBody] Document document)
		{
			try
			{
				var result = await _elasticService.IndexDocument(document);

				return Ok(new { message = "Document indexed successfully" });
			}
			catch (Exception)
			{
				return StatusCode(500, new { message = "Failed to index document"});
			}
		}

		// Query search
		[HttpPost("query-search", Name = "QuerySearch")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public async Task<IActionResult> SearchByQueryString([FromBody] string searchTerm)
		{
			if (string.IsNullOrWhiteSpace(searchTerm))
			{
				return BadRequest(new { message = "Search term cannot be empty" });
			}

			var result = await _elasticService.SearchByQueryString(searchTerm);

			return HandleSearchResponse(result);
		}

		// Fuzzy search
		[HttpPost("fuzzy-search", Name = "FuzzySearch")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public async Task<IActionResult> SearchByFuzzy([FromBody] string searchTerm)
		{
			if (string.IsNullOrWhiteSpace(searchTerm))
			{
				return BadRequest(new { message = "Search term cannot be empty" });
			}

			var result = await _elasticService.SearchByFuzzy(searchTerm);

			return HandleSearchResponse(result);
		}


		private IActionResult HandleSearchResponse(IReadOnlyCollection<Document>? result)
		{
			if (result == null)
				return NotFound(new { message = "No documents found matching the search term." });

			return Ok(result);
		}

		// Upload document
		[HttpPost(Name = "UploadDocument")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		public async Task<ActionResult> UploadDocumentAsync([FromForm] DocumentUploadDto documentUploadDto)
		{
			if (!ModelState.IsValid)
			{
				return BadRequest(ModelState);
			}

			var fileName = documentUploadDto.File.FileName;

			if (!fileName.EndsWith(".pdf"))
			{
				ModelState.AddModelError("file", "Only PDF files are allowed.");
				return BadRequest(ModelState);
			}

			var documentRequest = new DocumentRequestDto
			{
				Name = documentUploadDto.File.FileName,
				Size = documentUploadDto.File.Length
			};

			var document = _mapper.Map<Document>(documentRequest);

			uint newDocumentId = await _documentService.AddDocumentAsync(document);

			await minioService.UploadFileAsync(fileName, documentUploadDto.File.OpenReadStream());
			Console.WriteLine($@"File {fileName} uploaded to MinIO.");

			// save file in uploads folder
			var filePath = Path.Combine("/app/uploads", fileName);
			Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
			await using (var stream = new FileStream(filePath, FileMode.Create))
			{
				await documentUploadDto.File.CopyToAsync(stream);
			}

			_rabbitMQService.SendMessage(RabbitMQQueues.FileQueue, $"{newDocumentId}|{filePath}|{fileName}");
			Console.WriteLine($@"{fileName} an RabbitMQ Queue gesendet.");

			return Ok(document);
		}

		// Get document by Id
		[HttpGet("{id}", Name = "GetDocumentById")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public async Task<ActionResult<DocumentEntity>> GetDocumentById(int id)
		{
			try
			{
				var document = await _documentService.GetDocumentByIdAsync((uint)id);
				var documentResponse = _mapper.Map<DocumentResponseDto>(document);
				return Ok(documentResponse);
			}
			catch (KeyNotFoundException)
			{
				return NotFound();
			}
		}

		// Lists all documents
		[HttpGet(Name = "ListDocuments")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		public async Task<ActionResult> ListDocumentsAsync()
		{
			var documents = await _documentService.GetAllDocumentsAsync();
			var documentResponses = _mapper.Map<List<DocumentResponseDto>>(documents);
			return Ok(documentResponses);
		}

		// Delete a specific document based on id
		[HttpDelete("{id}", Name = "DeleteDocument")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[ProducesResponseType(StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult> DeleteDocumentAsync(int id)
		{
			try
			{
				var document = await _documentService.GetDocumentByIdAsync((uint)id);
				if (document == null)
				{
					return NotFound($"Document with ID {id} not found.");
				}

				var elasticDeleted = await _elasticService.DeleteDocumentAsync(document.Id.ToString());

				await _minioService.DeleteFileAsync(document.Name);

				await _documentService.DeleteDocumentByIdAsync((uint)id);

				return Ok($"Document with ID {id} and its associated file have been deleted.");
			}
			catch (KeyNotFoundException)
			{
				return NotFound($"Document with ID {id} not found.");
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { message = "Failed to delete document and associated file.", error = ex.Message });
			}
		}


		[HttpDelete("cleanup-index", Name = "CleanupIndex")]
		public async Task<IActionResult> CleanupIndex()
		{
			try
			{
				var response = await _elasticService.DeleteIndexAsync("documents");
				return Ok(new { message = "Index cleaned up successfully." });
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { message = "Failed to clean up index.", error = ex.Message });
			}
		}
	}
}
