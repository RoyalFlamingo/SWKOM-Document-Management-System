namespace Business.Models.Domain;

public record class User
{
	public required int Id { get; set; }
	public required string Name { get; set; }
	public required uint Role { get; set; }
}