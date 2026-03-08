using AutoMapper;
using Domain.Interfaces.Repositories;
using DomainModels.Models;
using EntityModels.Entities;
using Persistence.Context;

namespace Repository.Repositories;

public class NotebookRepository(AppDbContext context, IMapper mapper)
    : RepositoryBase<NotebookEntity, Notebook>(context, mapper), INotebookRepository
{
}
