namespace Business.Models.Domain;

public record class Document
{
	public required int Id { get; set; }
	public required string Name { get; set; }
	public required long Size { get; set; }
	public required DateTime UploadedAt { get; set; }
}