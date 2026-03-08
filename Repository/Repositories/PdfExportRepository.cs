using AutoMapper;
using Domain.Interfaces.Repositories;
using DomainModels.Models;
using EntityModels.Entities;
using Persistence.Context;

namespace Repository.Repositories;

public class PdfExportRepository(AppDbContext context, IMapper mapper)
    : RepositoryBase<PdfExportEntity, PdfExport>(context, mapper), IPdfExportRepository
{
}
