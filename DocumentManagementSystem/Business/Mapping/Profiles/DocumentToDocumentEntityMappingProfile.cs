using AutoMapper;
using Persistence.Models.Entities;
using Business.Models.Domain;

namespace Business.Mapping.Profiles;

public class DocumentToDocumentEntityMappingProfile : Profile
{
    public DocumentToDocumentEntityMappingProfile()
    {
        CreateMap<DocumentEntity, Document>();
        CreateMap<DocumentEntity, Document>().ReverseMap();
    }
}

