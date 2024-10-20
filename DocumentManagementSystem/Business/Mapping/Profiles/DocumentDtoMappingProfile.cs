using AutoMapper;
using Business.Models.Domain;

namespace Business.Mapping.Profiles;

public class DocumentDtoMappingProfile : Profile
{
	public DocumentDtoMappingProfile()
	{
		CreateMap<DocumentRequestDto, Document>();
		CreateMap<Document, DocumentResponseDto>();
	}
}
