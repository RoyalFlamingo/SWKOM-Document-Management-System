using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;

namespace DocumentManagementSystem.Controllers
{
	[ApiController]
	[ApiVersion("1")]
	[Route("api/v{version:apiVersion}/[controller]")]
	public class DocumentsController : ControllerBase
	{
		private readonly ILogger<DocumentsController> _logger;

		public DocumentsController(ILogger<DocumentsController> logger)
		{
			_logger = logger;
		}

		[HttpPost(Name = "UploadDocument")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		public async Task<ActionResult> UploadDocumentAsync(IFormFile file)
		{
			return Ok();
		}
	}
}
