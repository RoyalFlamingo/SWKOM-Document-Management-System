using Microsoft.AspNetCore.Http;

namespace Business.Models.Domain;

public class DocumentUploadDto
{
	public required IFormFile File { get; set; }
}