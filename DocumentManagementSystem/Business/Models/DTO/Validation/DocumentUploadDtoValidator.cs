using Business.Models.Domain;
using FluentValidation;

namespace Business.Models.DTO.Validation;

public class DocumentUploadDtoValidator : AbstractValidator<DocumentUploadDto>
{
	public DocumentUploadDtoValidator()
	{
		RuleFor(x => x.File)
			.NotNull().WithMessage("Please upload a file.")
			.Must(file => file.Length > 0).WithMessage("The uploaded file must not be empty.")
			.Must(file => file.Length <= 10 * 1024 * 1024).WithMessage("The file size must be less than 10 MB.")
			.Must(file => file.FileName.Length > 0).WithMessage("The file name must not empty.")
			.Must(file => file.FileName.Length < 255).WithMessage("The file name must exceed 255 characters.");
	}
}