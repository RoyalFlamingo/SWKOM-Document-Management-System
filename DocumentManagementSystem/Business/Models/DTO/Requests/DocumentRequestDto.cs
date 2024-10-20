namespace Business.Models.Domain;

public record class DocumentRequestDto
{
	public required string Name { get; set; }
	public required long Size { get; set; }
}