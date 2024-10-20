using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using Persistence.Models.Entities;
using Business.Services;

namespace DocumentManagementSystem.Controllers
{
	[ApiController]
	[ApiVersion("1")]
	[Route("api/v{version:apiVersion}/[controller]")]
	public class DocumentsController(ILogger<DocumentsController> logger, IDocumentService documentService) : ControllerBase
	{
		private readonly ILogger<DocumentsController> _logger = logger;
		private readonly IDocumentService _documentService = documentService;

		// Upload document
		[HttpPost(Name = "UploadDocument")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		public async Task<ActionResult> UploadDocumentAsync(IFormFile file)
		{
			// Testing
			var document = new DocumentEntity
			{
				Id = nextId++,
				Name = file.FileName,
				Size = file.Length,
				UploadedAt = DateTime.UtcNow
			};

			_documents.Add(document);

			return Ok(document);
		}

		// Get document by Id
		[HttpGet("{id}", Name = "GetDocumentById")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public async Task<ActionResult<DocumentEntity>> GetDocumentById(int id)
		{
			// Testing
			var document = _documents.FirstOrDefault(d => d.Id == id);
			if (document == null)
			{
				return NotFound();
			}

			return Ok(document);
		}

		// Lists all documents
		[HttpGet(Name = "ListDocuments")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		public async Task<ActionResult> ListDocumentsAsync()
		{
			return Ok(_documents);
		}

		// Delete a specific document based on id
		[HttpDelete("{id}", Name = "DeleteDocument")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public async Task<ActionResult> DeleteDocumentAsync(int id)
		{
			var document = _documents.FirstOrDefault(d => d.Id == id);
			if (document == null)
			{
				return NotFound();
			}

			_documents.Remove(document);
			return Ok($"Document with ID {id} has been deleted.");
		}
	}
}
