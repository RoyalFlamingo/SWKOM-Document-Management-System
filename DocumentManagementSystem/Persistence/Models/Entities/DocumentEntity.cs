using System.ComponentModel.DataAnnotations.Schema;

namespace Persistence.Models.Entities;

[Table("Documents")]
public record class DocumentEntity
{
	public int Id { get; set; }
	public string? Name { get; set; }
	public long Size { get; set; }
	public DateTime? UploadedAt { get; set; }
	public string? OcrContent { get; set; }
}